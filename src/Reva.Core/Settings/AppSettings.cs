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
    bool UseLlmAssist,
    string AiProvider = AiProviderNames.Ollama,
    string AiBaseUrl = AiSettingsDefaults.OllamaBaseUrl,
    string? AiApiKey = null,
    string AiModel = AiSettingsDefaults.DefaultModel)
{
    public static AppSettings Default => new(
        AppTheme.Dark,
        string.Empty,
        "Reva",
        0.6,
        0.85,
        null,
        0.01,
        false,
        AiProviderNames.Ollama,
        AiSettingsDefaults.OllamaBaseUrl,
        null,
        AiSettingsDefaults.DefaultModel);
}

public static class AiProviderNames
{
    public const string Ollama = "Ollama";
    public const string OpenAiCompatible = "OpenAiCompatible";
    public const string HuggingFace = "HuggingFace";

    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Ollama,
        OpenAiCompatible,
        HuggingFace
    };

    public static string Normalize(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Ollama;
        }

        var trimmed = provider.Trim();
        if (trimmed.Equals(OpenAiCompatible, StringComparison.OrdinalIgnoreCase))
        {
            return OpenAiCompatible;
        }

        if (trimmed.Equals(HuggingFace, StringComparison.OrdinalIgnoreCase))
        {
            return HuggingFace;
        }

        return trimmed.Equals(Ollama, StringComparison.OrdinalIgnoreCase) ? Ollama : Ollama;
    }
}

public static class AiSettingsDefaults
{
    public const string DefaultModel = "qwen2.5vl:7b";
    public const string OllamaBaseUrl = "http://localhost:11434/v1";
    public const string OpenAiCompatibleBaseUrl = "http://localhost:8080/v1";
    public const string HuggingFaceBaseUrl = "https://router.huggingface.co/v1";

    public static string BaseUrlFor(string provider) =>
        AiProviderNames.Normalize(provider) switch
        {
            AiProviderNames.OpenAiCompatible => OpenAiCompatibleBaseUrl,
            AiProviderNames.HuggingFace => HuggingFaceBaseUrl,
            _ => OllamaBaseUrl
        };

    public static string NormalizeBaseUrl(string provider, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return BaseUrlFor(provider);
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? trimmed
            : BaseUrlFor(provider);
    }

    public static string NormalizeModel(string? model) =>
        string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
}
