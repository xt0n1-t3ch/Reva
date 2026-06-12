using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed class DoclingDocumentParser(IOptions<DoclingParserOptions> options) : IDocumentParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var workerPath = ResolveWorkerPath();
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

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start parser worker.");
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
            throw new TimeoutException($"Parser worker exceeded {timeout.TotalSeconds:N0} seconds.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Parser worker failed: {stderr.Trim()}");
        }

        var payload = JsonSerializer.Deserialize<WorkerParseResult>(stdout, SerializerOptions)
            ?? throw new InvalidOperationException("Parser worker returned an empty payload.");

        var tables = payload.Tables
            .Select(table => new ExtractedTable(table.Name, table.Headers, table.Rows.Select(row => (IReadOnlyDictionary<string, string>)row).ToList()))
            .ToList();

        return new ParsedDocument(
            payload.ParserProfile,
            payload.SourceFormat,
            payload.Text,
            payload.Markdown,
            stdout,
            tables,
            payload.Warnings);
    }

    private string ResolveWorkerPath()
    {
        if (!string.IsNullOrWhiteSpace(options.Value.WorkerScriptPath))
        {
            return Path.GetFullPath(options.Value.WorkerScriptPath);
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

        throw new FileNotFoundException("Parser worker script not found.");
    }

    private sealed record WorkerParseResult(
        string ParserProfile,
        string SourceFormat,
        string Text,
        string Markdown,
        IReadOnlyList<WorkerTable> Tables,
        IReadOnlyList<string> Warnings);

    private sealed record WorkerTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<Dictionary<string, string>> Rows);
}

