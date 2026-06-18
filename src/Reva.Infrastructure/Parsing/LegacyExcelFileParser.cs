using System.Globalization;
using System.Text;
using ExcelDataReader;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed class LegacyExcelFileParser : IFileParser
{
    public string Profile => "excel-legacy";

    public bool CanParse(string extension) => extension == ".xls";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var tables = new List<ExtractedTable>();
        var markdown = new StringBuilder();
        var warnings = new List<string>();

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration
        {
            FallbackEncoding = System.Text.Encoding.Latin1
        });

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sheetName = reader.Name ?? $"Sheet{tables.Count + 1}";
            var allRows = new List<List<string>>();

            while (reader.Read())
            {
                var cells = new List<string>();
                for (var col = 0; col < reader.FieldCount; col++)
                {
                    var raw = reader.GetValue(col);
                    cells.Add(raw is null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                allRows.Add(cells);
            }

            if (allRows.Count == 0)
            {
                continue;
            }

            var headers = allRows[0].Select(h => h.Trim()).ToList();
            if (headers.All(string.IsNullOrWhiteSpace))
            {
                warnings.Add($"Sheet \"{sheetName}\" has no header row and was skipped.");
                continue;
            }

            var dataRows = allRows.Skip(1)
                .Select(cells => (IReadOnlyDictionary<string, string>)headers
                    .Select((header, i) => (header, value: i < cells.Count ? cells[i].Trim() : string.Empty))
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
                .ToList();

            tables.Add(ParseSupport.TableFromRows(sheetName, headers, dataRows));
            markdown.AppendLine(CultureInfo.InvariantCulture, $"## {sheetName}");
            markdown.AppendLine(ParseSupport.ToMarkdownTable(headers, dataRows));
            markdown.AppendLine();
        }
        while (reader.NextResult());

        var md = markdown.ToString().Trim();
        return Task.FromResult(ParseSupport.Build(Profile, "xls", md, md, tables, warnings.Count > 0 ? warnings : null));
    }
}
