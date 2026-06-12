namespace Reva.Infrastructure.Persistence;

public sealed class DocumentTableRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HeadersJson { get; set; } = "[]";
    public string RowsJson { get; set; } = "[]";
}

