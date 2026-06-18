using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed class OpenDocumentSpreadsheetParser : IFileParser
{
    private const string TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
    private const string TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    public string Profile => "ods";

    public bool CanParse(string extension) => extension == ".ods";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var tables = new List<ExtractedTable>();
        var markdown = new StringBuilder();
        var warnings = new List<string>();

        ZipArchive archive;
        try
        {
            archive = ZipFile.OpenRead(filePath);
        }
        catch (InvalidDataException ex)
        {
            return Task.FromResult(ParseSupport.Build(
                Profile, "ods", string.Empty, string.Empty,
                warnings: [$"Could not open ODS archive: {ex.Message}"]));
        }

        using (archive)
        {
            var entry = archive.GetEntry("content.xml");
            if (entry is null)
            {
                return Task.FromResult(ParseSupport.Build(
                    Profile, "ods", string.Empty, string.Empty,
                    warnings: ["ODS archive does not contain content.xml."]));
            }

            using var contentStream = entry.Open();
            var doc = new System.Xml.Linq.XDocument();
            try
            {
                doc = System.Xml.Linq.XDocument.Load(contentStream);
            }
            catch (XmlException ex)
            {
                return Task.FromResult(ParseSupport.Build(
                    Profile, "ods", string.Empty, string.Empty,
                    warnings: [$"ODS content.xml is malformed: {ex.Message}"]));
            }

            System.Xml.Linq.XNamespace tableNs = TableNs;
            System.Xml.Linq.XNamespace textNs = TextNs;

            var sheetElements = doc.Descendants(tableNs + "table").ToList();
            if (sheetElements.Count == 0)
            {
                return Task.FromResult(ParseSupport.Build(
                    Profile, "ods", string.Empty, string.Empty,
                    warnings: ["ODS file contains no table sheets."]));
            }

            foreach (var sheetEl in sheetElements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sheetName = (string?)sheetEl.Attribute(tableNs + "name") ?? $"Sheet{tables.Count + 1}";
                var rows = new List<List<string>>();

                foreach (var rowEl in sheetEl.Elements(tableNs + "table-row"))
                {
                    var cells = new List<string>();
                    foreach (var cellEl in rowEl.Elements(tableNs + "table-cell"))
                    {
                        var repeatAttr = (string?)cellEl.Attribute(tableNs + "number-columns-repeated");
                        var repeat = 1;
                        if (repeatAttr is not null && int.TryParse(repeatAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 1)
                        {
                            repeat = Math.Min(parsed, 256);
                        }

                        var cellText = string.Join(" ", cellEl.Descendants(textNs + "p").Select(p => p.Value)).Trim();

                        for (var r = 0; r < repeat; r++)
                        {
                            cells.Add(cellText);
                        }
                    }

                    if (cells.Any(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        rows.Add(cells);
                    }
                }

                if (rows.Count == 0)
                {
                    warnings.Add($"Sheet \"{sheetName}\" is empty and was skipped.");
                    continue;
                }

                var headers = rows[0].Select(h => h.Trim()).ToList();
                if (headers.All(string.IsNullOrWhiteSpace))
                {
                    warnings.Add($"Sheet \"{sheetName}\" has no header row and was skipped.");
                    continue;
                }

                var dataRows = rows.Skip(1)
                    .Select(cells => (IReadOnlyDictionary<string, string>)headers
                        .Select((header, i) => (header, value: i < cells.Count ? cells[i].Trim() : string.Empty))
                        .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                tables.Add(ParseSupport.TableFromRows(sheetName, headers, dataRows));
                markdown.AppendLine(CultureInfo.InvariantCulture, $"## {sheetName}");
                markdown.AppendLine(ParseSupport.ToMarkdownTable(headers, dataRows));
                markdown.AppendLine();
            }
        }

        var md = markdown.ToString().Trim();
        return Task.FromResult(ParseSupport.Build(Profile, "ods", md, md, tables, warnings.Count > 0 ? warnings : null));
    }
}
