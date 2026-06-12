namespace Reva.Infrastructure.Persistence;

public sealed class ReviewEventRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

