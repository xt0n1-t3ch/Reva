using Reva.Core.Settings;

namespace Reva.Infrastructure.Agent;

public sealed record AgentChatOptions
{
    public const string DefaultModel = AiSettingsDefaults.DefaultModel;
    public const string DefaultBaseUrl = AiSettingsDefaults.OllamaBaseUrl;
    public const int DefaultNumCtx = 16384;
    // Generous safety bound (runaway guard), not a flow limiter: the agent stops
    // naturally when the task is done. Configurable via Reva:Agent:MaxSteps.
    public const int DefaultMaxSteps = 48;
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
