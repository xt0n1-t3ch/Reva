namespace Reva.Infrastructure.Persistence;

public sealed class DocumentFieldRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsCorrected { get; set; }
}

