using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Reva.Core.Contracts;
using A = DocumentFormat.OpenXml.Drawing;

namespace Reva.Infrastructure.Parsing;

// Word .docx: paragraph text + any tables.
public sealed class WordFileParser : IFileParser
{
    public string Profile => "word";

    public bool CanParse(string extension) => extension == ".docx";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;
        var tables = new List<ExtractedTable>();
        var text = new StringBuilder();

        if (body is not null)
        {
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var line = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    text.AppendLine(line);
                }
            }

            var index = 1;
            foreach (var table in body.Elements<Table>())
            {
                var rows = table.Elements<TableRow>()
                    .Select(row => row.Elements<TableCell>().Select(cell => cell.InnerText.Trim()).ToList())
                    .Where(cells => cells.Count > 0)
                    .ToList();
                if (rows.Count == 0)
                {
                    continue;
                }

                var headers = rows[0];
                var dataRows = rows.Skip(1)
                    .Select(cells => (IReadOnlyDictionary<string, string>)headers
                        .Select((header, i) => (header, value: i < cells.Count ? cells[i] : string.Empty))
                        .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                tables.Add(ParseSupport.TableFromRows($"{ParseSupport.FriendlyName(filePath)} table {index++}", headers, dataRows));
            }
        }

        var plain = text.ToString().Trim();
        return Task.FromResult(ParseSupport.Build(Profile, "docx", plain, plain, tables));
    }
}

// PowerPoint .pptx: all slide text.
public sealed class PowerPointFileParser : IFileParser
{
    public string Profile => "powerpoint";

    public bool CanParse(string extension) => extension == ".pptx";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        using var document = PresentationDocument.Open(filePath, false);
        var slideParts = document.PresentationPart?.SlideParts ?? [];
        var text = new StringBuilder();
        var slide = 1;
        foreach (var part in slideParts)
        {
            var lines = (part.Slide?.Descendants<A.Text>() ?? []).Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t));
            text.AppendLine(CultureInfo.InvariantCulture, $"# Slide {slide++}");
            foreach (var line in lines)
            {
                text.AppendLine(line);
            }

            text.AppendLine();
        }

        var plain = text.ToString().Trim();
        return Task.FromResult(ParseSupport.Build(Profile, "pptx", plain, plain));
    }
}

// Excel .xlsx/.xlsm: one table per worksheet (header row + data).
public sealed class ExcelFileParser : IFileParser
{
    public string Profile => "excel";

    public bool CanParse(string extension) => extension is ".xlsx" or ".xlsm";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(filePath);
        var tables = new List<ExtractedTable>();
        var markdown = new StringBuilder();

        foreach (var sheet in workbook.Worksheets)
        {
            var used = sheet.RangeUsed();
            if (used is null)
            {
                continue;
            }

            var rows = used.RowsUsed().ToList();
            if (rows.Count == 0)
            {
                continue;
            }

            var headers = rows[0].Cells().Select(cell => cell.GetString().Trim()).ToList();
            if (headers.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var dataRows = rows.Skip(1)
                .Select(row => row.Cells().Select(cell => cell.GetString().Trim()).ToList())
                .Select(cells => (IReadOnlyDictionary<string, string>)headers
                    .Select((header, i) => (header, value: i < cells.Count ? cells[i] : string.Empty))
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
                .ToList();

            tables.Add(ParseSupport.TableFromRows(sheet.Name, headers, dataRows));
            markdown.AppendLine(CultureInfo.InvariantCulture, $"## {sheet.Name}");
            markdown.AppendLine(ParseSupport.ToMarkdownTable(headers, dataRows));
            markdown.AppendLine();
        }

        return Task.FromResult(ParseSupport.Build(Profile, "xlsx", markdown.ToString().Trim(), markdown.ToString().Trim(), tables));
    }
}
