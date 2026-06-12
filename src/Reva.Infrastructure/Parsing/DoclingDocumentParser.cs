using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed class DoclingDocumentParser(IOptions<DoclingParserOptions> options) : IDocumentParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> TextExtensions = [".txt", ".md"];
    private static readonly HashSet<string> CsvExtensions = [".csv"];
    private static readonly HashSet<string> BinaryTextExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff"];

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (TextExtensions.Contains(extension))
        {
            return await ParseTextAsync(filePath, extension, cancellationToken);
        }

        if (CsvExtensions.Contains(extension))
        {
            return await ParseCsvAsync(filePath, cancellationToken);
        }

        if (BinaryTextExtensions.Contains(extension))
        {
            return await TryParseWithWorkerAsync(filePath, cancellationToken)
                ?? await ParseBinaryVisibleTextAsync(filePath, extension, cancellationToken);
        }

        throw new InvalidOperationException($"Unsupported extension: {extension}.");
    }

    private static async Task<ParsedDocument> ParseTextAsync(string filePath, string extension, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        return BuildDocument("fallback-text", extension.TrimStart('.'), text, text, [], []);
    }

    private static async Task<ParsedDocument> ParseCsvAsync(string filePath, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return BuildDocument("fallback-csv", "csv", text, string.Empty, [], ["CSV document was empty after trimming blank lines."]);
        }

        var headers = ParseCsvLine(lines[0]);
        var rows = lines
            .Skip(1)
            .Select(ParseCsvLine)
            .Where(values => values.Count > 0)
            .Select(values => headers.Select((header, index) => new { header, value = index < values.Count ? values[index] : string.Empty })
                .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var tables = new List<ExtractedTable>
        {
            new(Path.GetFileNameWithoutExtension(filePath), headers, rows.Select(row => (IReadOnlyDictionary<string, string>)row).ToList())
        };
        return BuildDocument("fallback-csv", "csv", text, CsvToMarkdown(headers, rows), tables, []);
    }

    private async Task<ParsedDocument?> TryParseWithWorkerAsync(string filePath, CancellationToken cancellationToken)
    {
        var workerPath = TryResolveWorkerPath();
        if (workerPath is null)
        {
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo(options.Value.PythonExecutable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(workerPath);
            startInfo.ArgumentList.Add("--input");
            startInfo.ArgumentList.Add(filePath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var timeout = TimeSpan.FromSeconds(Math.Max(5, options.Value.TimeoutSeconds));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            var stdout = await stdoutTask;
            _ = await stderrTask;
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                return null;
            }

            var payload = JsonSerializer.Deserialize<WorkerParseResult>(stdout, SerializerOptions);
            return payload is null ? null : FromWorkerPayload(payload, stdout);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException or JsonException)
        {
            return null;
        }
    }

    private static async Task<ParsedDocument> ParseBinaryVisibleTextAsync(string filePath, string extension, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var decoded = Encoding.Latin1.GetString(bytes);
        var matches = Regex.Matches(decoded, @"[A-Za-z0-9][A-Za-z0-9 .,:;/%$#_\-]{2,}", RegexOptions.None, TimeSpan.FromSeconds(2));
        var text = string.Join('\n', matches.Select(match => match.Value.Trim()).Where(value => value.Length > 0));
        return BuildDocument(
            "fallback-binary-text",
            extension.TrimStart('.'),
            text,
            text,
            [],
            ["Docling worker was unavailable; binary document used fallback visible-text extraction."]);
    }

    private string? TryResolveWorkerPath()
    {
        if (!string.IsNullOrWhiteSpace(options.Value.WorkerScriptPath))
        {
            var configured = Path.GetFullPath(options.Value.WorkerScriptPath);
            return File.Exists(configured) ? configured : null;
        }

        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = Path.Combine(current, "tools", "docling-worker", "reva_docling_worker", "main.py");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        return null;
    }

    private static ParsedDocument FromWorkerPayload(WorkerParseResult payload, string rawJson)
    {
        var tables = payload.Tables
            .Select(table => new ExtractedTable(table.Name, table.Headers, table.Rows.Select(row => (IReadOnlyDictionary<string, string>)row).ToList()))
            .ToList();
        return new ParsedDocument(
            payload.ParserProfile,
            payload.SourceFormat,
            payload.Text,
            payload.Markdown,
            rawJson,
            tables,
            payload.Warnings);
    }

    private static ParsedDocument BuildDocument(
        string parserProfile,
        string sourceFormat,
        string text,
        string markdown,
        IReadOnlyList<ExtractedTable> tables,
        IReadOnlyList<string> warnings)
    {
        var payload = new WorkerParseResult(
            parserProfile,
            sourceFormat,
            text,
            markdown,
            tables.Select(table => new WorkerTable(
                table.Name,
                table.Headers,
                table.Rows.Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)).ToList())).ToList(),
            warnings);
        var rawJson = JsonSerializer.Serialize(payload, SerializerOptions);
        return new ParsedDocument(parserProfile, sourceFormat, text, markdown, rawJson, tables, warnings);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(value.ToString().Trim());
                value.Clear();
                continue;
            }

            value.Append(character);
        }

        values.Add(value.ToString().Trim());
        return values;
    }

    private static string CsvToMarkdown(List<string> headers, IReadOnlyList<Dictionary<string, string>> rows)
    {
        if (headers.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("| " + string.Join(" | ", headers.Select(EscapeMarkdownCell)) + " |");
        builder.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");
        foreach (var row in rows)
        {
            builder.AppendLine("| " + string.Join(" | ", headers.Select(header => EscapeMarkdownCell(row.GetValueOrDefault(header, string.Empty)))) + " |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string EscapeMarkdownCell(string value) => value.Replace("|", "\\|");

    private sealed record WorkerParseResult(
        string ParserProfile,
        string SourceFormat,
        string Text,
        string Markdown,
        IReadOnlyList<WorkerTable> Tables,
        IReadOnlyList<string> Warnings);

    private sealed record WorkerTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<Dictionary<string, string>> Rows);
}
