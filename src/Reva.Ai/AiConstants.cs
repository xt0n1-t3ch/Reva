namespace Reva.Ai;

public static class OllamaApi
{
    public const string TagsPath = "/api/tags";
    public const string OpenAiCompatibleSuffix = "/v1";
    public const string ApiKeyPlaceholder = "ollama";
}

public static class AiStatePaths
{
    public const string CompanyFolder = "Reva";
    public const string ModelStateFileName = "ai-model-state.json";

    public static string ModelStateFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.GetTempPath();
        }

        return Path.Combine(root, CompanyFolder, ModelStateFileName);
    }
}

public static class VlmExtractionDefaults
{
    public const int MaxPages = 8;
    public const int MaxTextCharacters = 8000;
    public const string PngMediaType = "image/png";
    public const string CitationToken = "vlm-citation";
    public const string SystemPrompt = "You read reinsurance bordereaux and contract documents from page images and return strict JSON only.";
}
