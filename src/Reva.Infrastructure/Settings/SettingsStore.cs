using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Settings;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.Settings;

public sealed partial class SettingsStore(RevaDbContext dbContext) : ISettingsStore
{
    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        var record = await dbContext.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.Id == AppSettingsRecord.SingletonId, cancellationToken);
        var settings = record is null ? AppSettings.Default : ToSettings(record);
        RuntimeSettings.Set(settings);
        return settings;
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var sanitized = Sanitize(settings);
        var record = await dbContext.AppSettings.FirstOrDefaultAsync(item => item.Id == AppSettingsRecord.SingletonId, cancellationToken);
        if (record is null)
        {
            record = new AppSettingsRecord { Id = AppSettingsRecord.SingletonId };
            dbContext.AppSettings.Add(record);
        }

        record.Theme = sanitized.Theme.ToString();
        record.AccentColor = sanitized.AccentColor;
        record.ProductName = sanitized.ProductName;
        record.ConfidenceLowMax = sanitized.ConfidenceLowMax;
        record.ConfidenceMediumMax = sanitized.ConfidenceMediumMax;
        record.DefaultTemplateId = sanitized.DefaultTemplateId;
        record.ReconciliationTolerance = sanitized.ReconciliationTolerance;
        record.UseLlmAssist = sanitized.UseLlmAssist;
        record.AiProvider = sanitized.AiProvider;
        record.AiBaseUrl = sanitized.AiBaseUrl;
        record.AiApiKey = sanitized.AiApiKey;
        record.AiModel = sanitized.AiModel;

        await dbContext.SaveChangesAsync(cancellationToken);
        RuntimeSettings.Set(sanitized);
        return sanitized;
    }

    // Keep stored settings safe and coherent: a valid hex accent (or none), a non-empty product
    // name, and ordered thresholds inside [0,1] so the colour injected into the page is trusted.
    private static AppSettings Sanitize(AppSettings settings)
    {
        var accent = HexColor().IsMatch(settings.AccentColor) ? settings.AccentColor.ToLowerInvariant() : string.Empty;
        var name = string.IsNullOrWhiteSpace(settings.ProductName) ? AppSettings.Default.ProductName : settings.ProductName.Trim();
        var low = Math.Clamp(settings.ConfidenceLowMax, 0d, 1d);
        var medium = Math.Clamp(settings.ConfidenceMediumMax, 0d, 1d);
        var reconciliationTolerance = Math.Clamp(settings.ReconciliationTolerance, 0d, 0.5d);
        var aiProvider = AiProviderNames.Normalize(settings.AiProvider);
        var aiBaseUrl = AiSettingsDefaults.NormalizeBaseUrl(aiProvider, settings.AiBaseUrl);
        var aiModel = AiSettingsDefaults.NormalizeModel(settings.AiModel);
        if (medium < low)
        {
            (low, medium) = (medium, low);
        }

        return settings with
        {
            AccentColor = accent,
            ProductName = name,
            ConfidenceLowMax = low,
            ConfidenceMediumMax = medium,
            ReconciliationTolerance = reconciliationTolerance,
            AiProvider = aiProvider,
            AiBaseUrl = aiBaseUrl,
            AiModel = aiModel
        };
    }

    private static AppSettings ToSettings(AppSettingsRecord record) => new(
        Enum.TryParse<AppTheme>(record.Theme, out var theme) ? theme : AppTheme.Light,
        record.AccentColor,
        record.ProductName,
        record.ConfidenceLowMax,
        record.ConfidenceMediumMax,
        record.DefaultTemplateId,
        record.ReconciliationTolerance,
        record.UseLlmAssist,
        AiProviderNames.Normalize(record.AiProvider),
        AiSettingsDefaults.NormalizeBaseUrl(record.AiProvider, record.AiBaseUrl),
        record.AiApiKey,
        AiSettingsDefaults.NormalizeModel(record.AiModel));

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();
}
