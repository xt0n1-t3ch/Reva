using System.Text.Json;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.Review;

public sealed class BdxReviewPayloadAssembler : IBdxReviewPayloadAssembler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public BdxReviewPayload Assemble(DocumentRecord document)
    {
        var spans = document.SourceSpans.Select(ToSpan).ToList();
        var pages = document.Pages.Count == 0
            ? [new BdxPage(1, $"/api/documents/{document.Id}/pages/1.png", 1, 1, 0)]
            : document.Pages.OrderBy(page => page.Page).Select(page => new BdxPage(page.Page, $"/api/documents/{document.Id}/pages/{page.Page}.png", page.Width, page.Height, page.Rotation)).ToList();
        var fields = document.Fields.Select(field => ToFieldValue(field, FindCitation(field.Value, spans), field.IsCorrected ? "user_confirmed" : Status(field))).ToList();
        var lineItems = document.Tables.SelectMany(table => ToLineItems(table, spans)).ToList();
        var reconciliation = document.Exceptions.Where(issue => issue.FieldName is not null).Select(issue => ToCheck(issue, spans)).ToList();
        return new BdxReviewPayload(new BdxDocument(document.Id, document.FileName, pages), spans, fields, lineItems, reconciliation);
    }

    private static SourceSpan ToSpan(DocumentSourceSpanRecord span) => new(
        span.SpanId,
        span.DocumentRecordId,
        span.Page,
        span.PageWidth,
        span.PageHeight,
        span.Rotation,
        new SourceBox(span.X, span.Y, span.Width, span.Height),
        string.IsNullOrWhiteSpace(span.PolygonJson) ? null : JsonSerializer.Deserialize<IReadOnlyList<SourcePoint>>(span.PolygonJson, SerializerOptions),
        span.Text,
        span.OcrConfidence,
        span.BlockId,
        span.TableId,
        span.RowIndex,
        span.ColumnIndex);

    private static FieldValue ToFieldValue(DocumentFieldRecord field, IReadOnlyList<Citation> citations, string status) => new(
        field.Name,
        field.Name,
        field.Value,
        field.Value,
        status,
        Math.Clamp(field.Confidence, 0, 1),
        new FieldProvenance(Method(field.Source), field.Source, null, null, citations));

    private static ReconciliationCheck ToCheck(DocumentIssueRecord issue, IReadOnlyList<SourceSpan> spans)
    {
        var expected = new FieldValue(issue.FieldName ?? "expected", issue.FieldName ?? "expected", issue.Expected ?? string.Empty, issue.Expected, "expected", issue.Confidence, new FieldProvenance("digital_parse", "reconciliation", null, null, []));
        var detected = new FieldValue(issue.FieldName ?? "detected", issue.FieldName ?? "detected", issue.Detected ?? string.Empty, issue.Detected, "detected", issue.Confidence, new FieldProvenance("digital_parse", "reconciliation", null, null, FindCitation(issue.Detected ?? string.Empty, spans)));
        return new ReconciliationCheck($"recon-{issue.Id}", issue.FieldName ?? "reconciliation", expected, detected, 0, 0, issue.Severity == "Critical" ? "fail" : "warning", issue.Message, detected.Provenance.Citations);
    }

    private static IEnumerable<LineItemValue> ToLineItems(DocumentTableRecord table, IReadOnlyList<SourceSpan> spans)
    {
        var headers = JsonSerializer.Deserialize<IReadOnlyList<string>>(table.HeadersJson, SerializerOptions) ?? [];
        var rows = JsonSerializer.Deserialize<IReadOnlyList<Dictionary<string, string>>>(table.RowsJson, SerializerOptions) ?? [];
        for (var i = 0; i < rows.Count; i++)
        {
            var fields = headers.Select(header =>
            {
                var value = rows[i].TryGetValue(header, out var found) ? found : string.Empty;
                return new FieldValue(header, header, value, value, string.IsNullOrWhiteSpace(value) ? "missing" : "detected", string.IsNullOrWhiteSpace(value) ? 0 : 0.75, new FieldProvenance("csv_parse", "table", null, null, FindCitation(value, spans)));
            }).ToList();
            yield return new LineItemValue($"{table.Id}-{i + 1}", i + 1, fields, fields.SelectMany(field => field.Provenance.Citations).Select(citation => citation.SourceSpanId).Distinct().ToList());
        }
    }

    private static IReadOnlyList<Citation> FindCitation(string value, IReadOnlyList<SourceSpan> spans)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var span = spans.FirstOrDefault(candidate => candidate.Text.Contains(value, StringComparison.OrdinalIgnoreCase))
            ?? spans.FirstOrDefault(candidate => value.Contains(candidate.Text, StringComparison.OrdinalIgnoreCase));
        return span is null ? [] : [new Citation(span.Id, span.Page, span.Bbox, span.Text, "value")];
    }

    private static string Status(DocumentFieldRecord field) => string.IsNullOrWhiteSpace(field.Value)
        ? "missing"
        : field.Confidence < 0.6 ? "low_confidence" : "detected";

    private static string Method(string source) => source switch
    {
        "table-header" => "csv_parse",
        "review" => "manual",
        _ when source.StartsWith("schema", StringComparison.OrdinalIgnoreCase) => "schema_mapping",
        _ => "digital_parse"
    };
}
