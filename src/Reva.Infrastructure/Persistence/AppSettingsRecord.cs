namespace Reva.Infrastructure.Persistence;

// A single-row table (Id is always 1) holding the app-wide settings.
public sealed class AppSettingsRecord
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string Theme { get; set; } = "Light";
    public string AccentColor { get; set; } = string.Empty;
    public string ProductName { get; set; } = "Reva";
    public double ConfidenceLowMax { get; set; } = 0.6;
    public double ConfidenceMediumMax { get; set; } = 0.85;
    public Guid? DefaultTemplateId { get; set; }
    public double ReconciliationTolerance { get; set; } = 0.01;
    public bool UseLlmAssist { get; set; }
}
