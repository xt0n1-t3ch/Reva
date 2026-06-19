namespace Reva.Infrastructure.Persistence;

public sealed class LearnedSchemaMappingRecord
{
    public int Id { get; set; }
    public string SenderKey { get; set; } = string.Empty;
    public string SourceHeader { get; set; } = string.Empty;
    public string NormalizedSourceHeader { get; set; } = string.Empty;
    public string CanonicalField { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public bool IsOverride { get; set; }
    public int UseCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
