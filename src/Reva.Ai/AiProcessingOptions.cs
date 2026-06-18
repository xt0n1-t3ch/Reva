namespace Reva.Ai;

public sealed class AiProcessingOptions
{
    public const string DefaultBaseUrl = "http://localhost:11434";
    public const string DefaultOpenAiBaseUrl = "http://localhost:11434/v1";
    public const string DefaultActiveModel = "qwen3-vl:8b";
    public const bool DefaultUseVisionExtraction = false;
    public const int DefaultTimeoutSeconds = 120;

    public const string BaseUrlKey = "Reva:Ai:BaseUrl";
    public const string OpenAiBaseUrlKey = "Reva:Ai:OpenAiBaseUrl";
    public const string ActiveModelKey = "Reva:Ai:ActiveModel";
    public const string UseVisionKey = "Reva:Ai:UseVision";
    public const string TimeoutSecondsKey = "Reva:Ai:TimeoutSeconds";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string OpenAiBaseUrl { get; set; } = DefaultOpenAiBaseUrl;
    public string ActiveModel { get; set; } = DefaultActiveModel;
    public bool UseVisionExtraction { get; set; } = DefaultUseVisionExtraction;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}
