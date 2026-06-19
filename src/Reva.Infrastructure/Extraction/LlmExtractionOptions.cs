namespace Reva.Infrastructure.Extraction;

public sealed class LlmExtractionOptions
{
    public const string ProviderNone = "None";
    public const string ProviderOllama = "Ollama";
    public const string PromptVersion = "bdx-review-v1";
    public const string SchemaVersion = "bdx-review-payload-v1";
    public const string DefaultBaseUrl = "http://localhost:11434/v1";
    public const string DefaultModel = "qwen2.5vl:7b";
    public const int MaxPromptCharacters = 8000;
    public const int RetryPromptCharacters = 3000;
    public const string SystemPrompt = "You extract reinsurance bordereaux fields and return strict JSON only.";
    public string Provider { get; set; } = ProviderNone;
    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string Model { get; set; } = DefaultModel;
    public bool DeterministicOnly { get; set; } = true;
}
