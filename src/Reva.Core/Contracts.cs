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

public sealed record ExtractionIssue(ExceptionSeverity Severity, string Message);

public sealed record FieldCorrection(string Name, string Value);

public sealed record ReviewDecision(string Decision, string Reviewer, string? Notes, IReadOnlyList<FieldCorrection> FieldCorrections);

public sealed record ExportRecord(
    Guid DocumentId,
    ReinsuranceDocumentType DocumentType,
    ReviewState ReviewState,
    IReadOnlyDictionary<string, string> Fields,
    DateTimeOffset ExportedAt);


