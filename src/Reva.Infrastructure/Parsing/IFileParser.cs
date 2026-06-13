using System.Text;
using System.Text.RegularExpressions;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

// One small parser per file family. The router picks the first that claims the file;
// anything unclaimed (or that fails) falls back to best-effort visible text.
public interface IFileParser
{
    string Profile { get; }
    bool CanParse(string extension);
    Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken);
}

// Shared building blocks so every parser produces a consistent ParsedDocument.
public static partial class ParseSupport
{
    public static ParsedDocument Build(
        string profile,
        string sourceFormat,
        string text,
        string markdown,
        IReadOnlyList<ExtractedTable>? tables = null,
        IReadOnlyList<string>? warnings = null) =>
        new(profile, sourceFormat, text ?? string.Empty, markdown ?? text ?? string.Empty,
            string.Empty, tables ?? [], warnings ?? []);

    // Stored files are named "{guid:N}-{original}". Show the original name only.
    public static string FriendlyName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var match = GuidPrefix().Match(name);
        return match.Success ? name[match.Length..] : name;
    }

    public static ExtractedTable TableFromRows(string name, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyDictionary<string, string>> rows) =>
        new(name, headers, rows);

    public static string ToMarkdownTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        if (headers.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("| " + string.Join(" | ", headers.Select(EscapeCell)) + " |");
        builder.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");
        foreach (var row in rows)
        {
            builder.AppendLine("| " + string.Join(" | ", headers.Select(h => EscapeCell(row.TryGetValue(h, out var v) ? v : string.Empty))) + " |");
        }

        return builder.ToString().TrimEnd();
    }

    public static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
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

            if (c == ',' && !inQuotes)
            {
                values.Add(value.ToString().Trim());
                value.Clear();
                continue;
            }

            value.Append(c);
        }

        values.Add(value.ToString().Trim());
        return values;
    }

    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    [GeneratedRegex("^[0-9a-fA-F]{32}-", RegexOptions.CultureInvariant)]
    private static partial Regex GuidPrefix();
}
