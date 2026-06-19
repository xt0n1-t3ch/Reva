using System.Globalization;
using System.Text.Json;
using Reva.Core.Contracts;

namespace Reva.Web.Endpoints;

internal static class DocumentProcessingStream
{
    private static readonly JsonSerializerOptions FrameOptions = new(JsonSerializerDefaults.Web);
    private const int MaxLines = 400;
    private const int LineDelayMs = 16;
    private const int StageDelayMs = 110;

    public static async Task WriteAsync(HttpResponse response, string fileName, string parsedText, BdxReviewPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var scanLines = BuildScanLines(parsedText, payload);

            await StageAsync(response, "parsing", "start", $"Reading {fileName}", cancellationToken);
            await Task.Delay(StageDelayMs, cancellationToken);
            await StageAsync(response, "parsing", "done", $"Read {scanLines.Count} lines of source text", cancellationToken);

            await StageAsync(response, "ocr", "start", "Scanning the document line by line", cancellationToken);
            var streamed = 0;
            foreach (var line in scanLines)
            {
                await SendAsync(response, new { type = "line", page = line.Page, text = line.Text, at = Now() }, cancellationToken);
                if (++streamed >= MaxLines)
                {
                    break;
                }

                await Task.Delay(LineDelayMs, cancellationToken);
            }

            await StageAsync(response, "ocr", "done", $"Scanned {streamed} lines", cancellationToken);

            await StageAsync(response, "extracting", "start", "Locating reinsurance fields", cancellationToken);
            var capturedFields = 0;
            foreach (var field in payload.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                {
                    continue;
                }

                int? page = field.Provenance.Citations.Count > 0 ? field.Provenance.Citations[0].Page : null;
                await SendAsync(response, new { type = "field", field = field.Label, value = field.Value, confidence = field.Confidence, page, at = Now() }, cancellationToken);
                capturedFields++;
                await Task.Delay(StageDelayMs, cancellationToken);
            }

            await StageAsync(response, "extracting", "done", $"Captured {capturedFields} fields", cancellationToken);

            await StageAsync(response, "mapping", "start", "Mapping headers to canonical fields", cancellationToken);
            await Task.Delay(StageDelayMs, cancellationToken);
            await StageAsync(response, "mapping", "done", "Schema mapped", cancellationToken);

            await StageAsync(response, "reconciling", "start", "Checking stated totals against line items", cancellationToken);
            foreach (var check in payload.Reconciliation)
            {
                await SendAsync(response, new { type = "reconcile", field = check.Name, detected = check.Detected.Value, expected = check.Expected.Value, agreement = Agreement(check.Status), at = Now() }, cancellationToken);
                await Task.Delay(StageDelayMs, cancellationToken);
            }

            await StageAsync(response, "reconciling", "done", $"{payload.Reconciliation.Count} reconciliation checks", cancellationToken);

            await SendAsync(response, new { type = "done", documentId = payload.Document.Id, at = Now() }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }
        catch (Exception ex)
        {
            await TrySendAsync(response, new { type = "error", message = ex.Message, at = Now() });
        }
        finally
        {
            await TrySentinelAsync(response);
        }
    }

    private static Task StageAsync(HttpResponse response, string stage, string status, string detail, CancellationToken cancellationToken) =>
        SendAsync(response, new { type = "stage", stage, status, detail, at = Now() }, cancellationToken);

    private static async Task SendAsync(HttpResponse response, object frame, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(frame, FrameOptions);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    private static async Task TrySendAsync(HttpResponse response, object frame)
    {
        try
        {
            await SendAsync(response, frame, CancellationToken.None);
        }
        catch (Exception)
        {
            // best-effort: the client may already be gone
        }
    }

    private static async Task TrySentinelAsync(HttpResponse response)
    {
        try
        {
            await response.WriteAsync("data: [DONE]\n\n", CancellationToken.None);
            await response.Body.FlushAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // best-effort terminal frame
        }
    }

    private readonly record struct ScanLine(int Page, string Text);

    private static List<ScanLine> BuildScanLines(string parsedText, BdxReviewPayload payload)
    {
        var lines = new List<ScanLine>();
        foreach (var span in payload.SourceSpans)
        {
            var text = span.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                lines.Add(new ScanLine(span.Page, text));
            }
        }

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(parsedText))
        {
            foreach (var raw in parsedText.Split('\n'))
            {
                var text = raw.Trim();
                if (!string.IsNullOrEmpty(text) && !IsSeparatorLine(text))
                {
                    lines.Add(new ScanLine(1, text));
                }
            }
        }

        return lines;
    }

    private static bool IsSeparatorLine(string text)
    {
        foreach (var character in text)
        {
            if (character is not ('|' or '-' or ':' or ' '))
            {
                return false;
            }
        }

        return true;
    }

    private static double Agreement(string status) => status switch
    {
        "fail" => 0.5,
        "warning" => 0.85,
        _ => 0.99,
    };

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
}
