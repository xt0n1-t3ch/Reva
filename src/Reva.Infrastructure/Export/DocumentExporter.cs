using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Reva.Core.Contracts;
using Reva.Core.Export;

namespace Reva.Infrastructure.Export;

// Applies an export template to a document's canonical data. A column whose source matches a
// line-item table header is pulled per row; otherwise it is a document-level field repeated on
// every row. If no column references the table, the export is a single document-level row.
public sealed class DocumentExporter : IDocumentExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public ExportPreview Preview(DocumentDetail document, ExportTemplate layout, int maxRows = 6)
    {
        var (headers, rows) = Render(document, layout);
        return new ExportPreview(headers, [.. rows.Take(Math.Max(0, maxRows))]);
    }

    public ExportFile Export(DocumentDetail document, ExportTemplate layout)
    {
        var (headers, rows) = Render(document, layout);
        var stem = $"reva-{Slug(layout.Name)}-{document.Id:N}";

        return layout.Format switch
        {
            ExportFormat.Excel => new ExportFile(BuildExcel(headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{stem}.xlsx"),
            ExportFormat.Json => new ExportFile(BuildJson(headers, rows), "application/json", $"{stem}.json"),
            _ => new ExportFile(Encoding.UTF8.GetBytes(BuildCsv(headers, rows)), "text/csv", $"{stem}.csv")
        };
    }

    private static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) Render(DocumentDetail document, ExportTemplate template)
    {
        var headers = template.Columns.Select(column => column.Header).ToList();
        var fields = document.Fields.ToDictionary(field => field.Name, field => field.Value, StringComparer.OrdinalIgnoreCase);
        var table = document.Tables.FirstOrDefault(candidate => candidate.Rows.Count > 0);

        // Pre-resolve each column to either a table header (per-row) or a field (document-level).
        var resolved = template.Columns
            .Select(column => new
            {
                column.Source,
                TableHeader = table?.Headers.FirstOrDefault(header => header.Equals(column.Source, StringComparison.OrdinalIgnoreCase))
            })
            .ToList();

        var usesTable = table is not null && resolved.Any(column => column.TableHeader is not null);

        if (usesTable)
        {
            var rows = table!.Rows
                .Select(row => (IReadOnlyList<string>)resolved
                    .Select(column => column.TableHeader is not null
                        ? (row.TryGetValue(column.TableHeader, out var cell) ? cell : string.Empty)
                        : (fields.TryGetValue(column.Source, out var value) ? value : string.Empty))
                    .ToList())
                .ToList();
            return (headers, rows);
        }

        var single = (IReadOnlyList<string>)resolved
            .Select(column => fields.TryGetValue(column.Source, out var value) ? value : string.Empty)
            .ToList();
        return (headers, [single]);
    }

    private static byte[] BuildExcel(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Export");
        for (var c = 0; c < headers.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        sheet.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = row[c];
            }
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildJson(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var objects = rows
            .Select(row =>
            {
                var item = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < headers.Count && c < row.Count; c++)
                {
                    item[headers[c]] = row[c];
                }

                return item;
            })
            .ToList();
        return JsonSerializer.SerializeToUtf8Bytes(objects, SerializerOptions);
    }

    private static string BuildCsv(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',', StringComparison.Ordinal) && !value.Contains('"', StringComparison.Ordinal) && !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Slug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
