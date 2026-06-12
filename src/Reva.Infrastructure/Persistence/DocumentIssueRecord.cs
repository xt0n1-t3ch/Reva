namespace Reva.Infrastructure.Persistence;

public sealed class DocumentIssueRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

