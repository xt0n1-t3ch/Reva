namespace Reva.Infrastructure.Persistence;

public sealed class DocumentPageRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public int Page { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
    public int Rotation { get; set; }
}
