namespace Reva.Infrastructure.Persistence;

public sealed class DocumentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReviewState { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string ParsedMarkdown { get; set; } = string.Empty;
    public string ParsedJson { get; set; } = string.Empty;
    public string ParserProfile { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<DocumentFieldRecord> Fields { get; set; } = [];
    public List<DocumentTableRecord> Tables { get; set; } = [];
    public List<DocumentSchemaMappingRecord> SchemaMappings { get; set; } = [];
    public List<DocumentSourceSpanRecord> SourceSpans { get; set; } = [];
    public List<DocumentPageRecord> Pages { get; set; } = [];
    public List<DocumentIssueRecord> Exceptions { get; set; } = [];
    public List<ReviewEventRecord> ReviewEvents { get; set; } = [];
}
