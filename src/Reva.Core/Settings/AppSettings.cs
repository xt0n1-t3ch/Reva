namespace Reva.Core.Settings;

// The app-wide, user-customizable settings. One row, persisted; loaded into RuntimeSettings at
// startup and on every save so the whole UI reflects it.
public sealed record AppSettings(
    AppTheme Theme,
    string AccentColor,          // "#rrggbb" to recolor the accent, or empty for the built-in default
    string ProductName,
    double ConfidenceLowMax,     // score below this renders as "Low"
    double ConfidenceMediumMax,  // score below this renders as "Medium"; at or above is "High"
    Guid? DefaultTemplateId,
    double ReconciliationTolerance,
    bool UseLlmAssist)
{
    public static AppSettings Default => new(AppTheme.Light, string.Empty, "Reva", 0.6, 0.85, null, 0.01, false);
}
