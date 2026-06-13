using System.Text;
using System.Text.RegularExpressions;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

// Plain text and Markdown: read as UTF-8 text.
public sealed class TextFileParser : IFileParser
{
    public string Profile => "text";

    public bool CanParse(string extension) => extension is ".txt" or ".md" or ".log";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        return ParseSupport.Build(Profile, "text", text, text);
    }
}

// CSV: first row is the header, remaining rows become one table.
public sealed class CsvFileParser : IFileParser
{
    public string Profile => "csv";

    public bool CanParse(string extension) => extension is ".csv" or ".tsv";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var raw = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
        var lines = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return ParseSupport.Build(Profile, "csv", raw, string.Empty, warnings: ["CSV document was empty after trimming blank lines."]);
        }

        var headers = ParseSupport.SplitCsvLine(lines[0]);
        var rows = lines.Skip(1)
            .Select(ParseSupport.SplitCsvLine)
            .Where(values => values.Count > 0)
            .Select(values => (IReadOnlyDictionary<string, string>)headers
                .Select((header, index) => (header, value: index < values.Count ? values[index] : string.Empty))
                .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var table = ParseSupport.TableFromRows(ParseSupport.FriendlyName(filePath), headers, rows);
        var markdown = ParseSupport.ToMarkdownTable(headers, rows);
        return ParseSupport.Build(Profile, "csv", raw, markdown, [table]);
    }
}

// Last resort: pull human-readable text out of any bytes. Never throws, always low value.
public sealed partial class BinaryFallbackParser : IFileParser
{
    public string Profile => "binary-fallback";

    public bool CanParse(string extension) => true;

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var decoded = Encoding.Latin1.GetString(bytes);
        var matches = VisibleText().Matches(decoded);
        var text = string.Join('\n', matches.Select(m => m.Value.Trim()).Where(v => v.Length > 0));
        var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        return ParseSupport.Build(
            Profile,
            string.IsNullOrEmpty(extension) ? "binary" : extension,
            text,
            text,
            warnings: ["No specialized parser handled this file; extracted visible text only."]);
    }

    [GeneratedRegex(@"[A-Za-z0-9][A-Za-z0-9 .,:;/%$#_\-]{2,}", RegexOptions.CultureInvariant, 2000)]
    private static partial Regex VisibleText();
}
