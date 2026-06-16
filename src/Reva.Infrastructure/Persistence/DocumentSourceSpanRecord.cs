namespace Reva.Infrastructure.Persistence;

public sealed class DocumentSourceSpanRecord
{
    public int Id { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string SpanId { get; set; } = string.Empty;
    public int Page { get; set; }
    public double PageWidth { get; set; }
    public double PageHeight { get; set; }
    public int Rotation { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string PolygonJson { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double? OcrConfidence { get; set; }
    public string? BlockId { get; set; }
    public string? TableId { get; set; }
    public int? RowIndex { get; set; }
    public int? ColumnIndex { get; set; }
}
