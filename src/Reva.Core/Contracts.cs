using Reva.Core.Documents;
using Reva.Core.Reinsurance;

namespace Reva.Core.Contracts;

public sealed record DocumentUploadResult(
    Guid Id,
    string FileName,
    string Sha256Hash,
    DocumentStatus Status,
    DateTimeOffset CreatedAt);

public sealed record DocumentSummary(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    ReviewState ReviewState,
    ReinsuranceDocumentType DocumentType,
    double Confidence,
    int ExceptionCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentDetail(
    Guid Id,
    string FileName,
    string Sha256Hash,
    DocumentStatus Status,
    ReviewState ReviewState,
    ReinsuranceDocumentType DocumentType,
    double Confidence,
    string ParsedMarkdown,
    string ParserProfile,
    IReadOnlyList<ExtractedField> Fields,
    IReadOnlyList<ExtractedTable> Tables,
    IReadOnlyList<ExtractionIssue> Exceptions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExtractedField(string Name, string Value, double Confidence, string Source, bool IsCorrected);

public sealed record ExtractedTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

// An extraction or reconciliation finding. Generic findings (missing field, unclassified)
// carry only Severity + Message. Reconciliation findings also carry the field that
// disagreed, the value the document stated (Detected), the value computed from the line
// items (Expected), and an agreement confidence in [0,1] (1 = perfect agreement).
public sealed record ExtractionIssue(
    ExceptionSeverity Severity,
    string Message,
    string? FieldName = null,
    string? Detected = null,
    string? Expected = null,
    double Confidence = 0d)
{
    // True when this finding compares a stated value against a computed one.
    public bool IsReconciliation => FieldName is not null && Detected is not null && Expected is not null;
}

public sealed record FieldCorrection(string Name, string Value);

public sealed record ReviewDecision(string Decision, string Reviewer, string? Notes, IReadOnlyList<FieldCorrection> FieldCorrections);

public sealed record ExportRecord(
    Guid DocumentId,
    ReinsuranceDocumentType DocumentType,
    ReviewState ReviewState,
    IReadOnlyDictionary<string, string> Fields,
    DateTimeOffset ExportedAt);


