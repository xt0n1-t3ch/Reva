namespace Reva.Infrastructure.Persistence;

public sealed class DocumentSchemaMappingRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string SenderKey { get; set; } = string.Empty;
    public string SourceHeader { get; set; } = string.Empty;
    public string NormalizedSourceHeader { get; set; } = string.Empty;
    public string CanonicalField { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsLearned { get; set; }
    public bool IsCorrected { get; set; }
}
