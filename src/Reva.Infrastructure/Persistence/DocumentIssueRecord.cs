namespace Reva.Infrastructure.Persistence;

public sealed class DocumentIssueRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Reconciliation findings only: the canonical field that disagreed, the value stated
    // in the document, the value computed from the line items, and an agreement score [0,1].
    public string? FieldName { get; set; }
    public string? Detected { get; set; }
    public string? Expected { get; set; }
    public double Confidence { get; set; }
}

