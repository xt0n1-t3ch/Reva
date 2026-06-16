namespace Reva.Core.Contracts;

public sealed record SourceBox(double X, double Y, double Width, double Height);

public sealed record SourcePoint(double X, double Y);

public sealed record SourceSpan(
    string Id,
    Guid DocumentId,
    int Page,
    double PageWidth,
    double PageHeight,
    int Rotation,
    SourceBox Bbox,
    IReadOnlyList<SourcePoint>? Polygon,
    string Text,
    double? OcrConfidence,
    string? BlockId,
    string? TableId,
    int? RowIndex,
    int? ColumnIndex);

public sealed record Citation(
    string SourceSpanId,
    int Page,
    SourceBox Bbox,
    string? Quote,
    string Role);

public sealed record FieldProvenance(
    string Method,
    string StepId,
    string? Model,
    string? PromptVersion,
    IReadOnlyList<Citation> Citations);

public sealed record FieldValue(
    string Key,
    string Label,
    string Value,
    string? RawText,
    string Status,
    double Confidence,
    FieldProvenance Provenance);

public sealed record ReconciliationCheck(
    string Id,
    string Name,
    FieldValue Expected,
    FieldValue Detected,
    double Delta,
    double Tolerance,
    string Status,
    string Explanation,
    IReadOnlyList<Citation> Citations);

public sealed record BdxPage(int Page, string ImageUrl, double Width, double Height, int Rotation);

public sealed record BdxDocument(Guid Id, string Filename, IReadOnlyList<BdxPage> Pages);

public sealed record LineItemValue(string Id, int RowNumber, IReadOnlyList<FieldValue> Fields, IReadOnlyList<string> RowCitationIds);

public sealed record BdxReviewPayload(
    BdxDocument Document,
    IReadOnlyList<SourceSpan> SourceSpans,
    IReadOnlyList<FieldValue> Fields,
    IReadOnlyList<LineItemValue> LineItems,
    IReadOnlyList<ReconciliationCheck> Reconciliation);
