namespace Reva.Infrastructure.Extraction;

public sealed class LlmExtractionOptions
{
    public const string ProviderNone = "None";
    public const string ProviderOllama = "Ollama";
    public const string PromptVersion = "bdx-review-v1";
    public const string SchemaVersion = "bdx-review-payload-v1";
    public string Provider { get; set; } = ProviderNone;
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "qwen3-vl:8b";
    public bool DeterministicOnly { get; set; } = true;
}
