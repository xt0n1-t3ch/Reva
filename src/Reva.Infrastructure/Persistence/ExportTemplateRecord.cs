namespace Reva.Infrastructure.Persistence;

public sealed class ExportTemplateRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = "Csv";

    // The ordered columns (header + source) serialized as JSON.
    public string ColumnsJson { get; set; } = "[]";

    // Built-in templates ship with the app and cannot be deleted.
    public bool IsBuiltIn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
