using Reva.Core.Documents;
using Reva.Core.Export;
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
    DateTimeOffset UpdatedAt)
{
    public IReadOnlyList<SchemaMapping> SchemaMappings { get; init; } = [];
}

public sealed record ExtractedField(string Name, string Value, double Confidence, string Source, bool IsCorrected);

public sealed record ExtractedTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

public sealed record SchemaMapping(
    string SenderKey,
    string SourceHeader,
    string CanonicalField,
    string NormalizedValue,
    double Confidence,
    string Source,
    bool IsLearned,
    bool IsCorrected);

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

public sealed record SchemaMappingCorrection(string SourceHeader, string CanonicalField);

public sealed record ReviewDecision(string Decision, string Reviewer, string? Notes, IReadOnlyList<FieldCorrection> FieldCorrections)
{
    public IReadOnlyList<SchemaMappingCorrection> MappingCorrections { get; init; } = [];
}

public sealed record ExportRecord(
    Guid DocumentId,
    ReinsuranceDocumentType DocumentType,
    ReviewState ReviewState,
    IReadOnlyDictionary<string, string> Fields,
    DateTimeOffset ExportedAt);

// One output column: the header the company wants, and the canonical field or table column
// it pulls from.
public sealed record ExportColumn(string Header, string Source);

// A reusable, customizable export layout. Built-in templates ship with the app and cannot be
// deleted; user templates are fully editable.
public sealed record ExportTemplate(
    Guid Id,
    string Name,
    ExportFormat Format,
    IReadOnlyList<ExportColumn> Columns,
    bool IsBuiltIn);

// The editable shape used to create or update a template.
public sealed record ExportTemplateDraft(string Name, ExportFormat Format, IReadOnlyList<ExportColumn> Columns);

// A small rendered sample of what a template will produce, for the live preview.
public sealed record ExportPreview(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

// A rendered export ready to download.
public sealed record ExportFile(byte[] Content, string ContentType, string FileName);

