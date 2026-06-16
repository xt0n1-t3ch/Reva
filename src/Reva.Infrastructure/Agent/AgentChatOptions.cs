namespace Reva.Infrastructure.Agent;

public sealed record AgentChatOptions
{
    public const string DefaultModel = "qwen3-vl:8b";
    public const string DefaultBaseUrl = "http://localhost:11434/v1";
    public const int DefaultNumCtx = 16384;
    public const int DefaultMaxSteps = 6;
    public const double DefaultTemperature = 0;
    public const int DefaultMaxMessages = 50;
    public const int DefaultMaxRequestBytes = 1_000_000;
    public const string NumCtxPropertyName = "num_ctx";

    public string Model { get; set; } = DefaultModel;
    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public int NumCtx { get; set; } = DefaultNumCtx;
    public int MaxSteps { get; set; } = DefaultMaxSteps;
    public double Temperature { get; set; } = DefaultTemperature;
    public int MaxMessages { get; set; } = DefaultMaxMessages;
    public int MaxRequestBytes { get; set; } = DefaultMaxRequestBytes;
}
