using Microsoft.Extensions.AI;

namespace Reva.Infrastructure.Agent;

public enum AgentReasoningLevel
{
    Off,
    Low,
    Medium,
    High,
    Max
}

public sealed record AgentReasoningOptions(AgentReasoningLevel Level)
{
    public const string HeaderName = "x-reva-reasoning";
    public const string ReasoningEffortPropertyName = "reasoning_effort";
    public const string EnableThinkingPropertyName = "enable_thinking";
    public const string ExtraBodyPropertyName = "extra_body";

    public static AgentReasoningOptions Default { get; } = new(AgentReasoningLevel.Medium);

    public static AgentReasoningOptions FromHeader(string? value) =>
        value?.Trim() switch
        {
            "OFF" or "Off" or "off" => new AgentReasoningOptions(AgentReasoningLevel.Off),
            "Low" or "low" or "LOW" => new AgentReasoningOptions(AgentReasoningLevel.Low),
            "Medium" or "medium" or "MEDIUM" => new AgentReasoningOptions(AgentReasoningLevel.Medium),
            "High" or "high" or "HIGH" => new AgentReasoningOptions(AgentReasoningLevel.High),
            "Max" or "max" or "MAX" => new AgentReasoningOptions(AgentReasoningLevel.Max),
            _ => Default
        };
}

public static class AgentReasoningMapper
{
    public static AdditionalPropertiesDictionary BuildAdditionalProperties(
        int numCtx,
        string provider,
        string model,
        AgentReasoningOptions? reasoning)
    {
        var properties = new AdditionalPropertiesDictionary
        {
            [AgentChatOptions.NumCtxPropertyName] = numCtx
        };

        ApplyReasoning(properties, provider, model, reasoning ?? AgentReasoningOptions.Default);
        return properties;
    }

    private static void ApplyReasoning(
        AdditionalPropertiesDictionary properties,
        string provider,
        string model,
        AgentReasoningOptions reasoning)
    {
        switch (reasoning.Level)
        {
            case AgentReasoningLevel.Off:
                if (IsThinkingCapable(provider, model))
                {
                    SetThinking(properties, false);
                }

                break;
            case AgentReasoningLevel.Low:
                properties[AgentReasoningOptions.ReasoningEffortPropertyName] = "low";
                break;
            case AgentReasoningLevel.Medium:
                properties[AgentReasoningOptions.ReasoningEffortPropertyName] = "medium";
                if (IsThinkingCapable(provider, model))
                {
                    SetThinking(properties, true);
                }

                break;
            case AgentReasoningLevel.High:
                properties[AgentReasoningOptions.ReasoningEffortPropertyName] = "high";
                if (IsThinkingCapable(provider, model))
                {
                    SetThinking(properties, true);
                }

                break;
            case AgentReasoningLevel.Max:
                properties[AgentReasoningOptions.ReasoningEffortPropertyName] = "high";
                if (IsThinkingCapable(provider, model))
                {
                    SetThinking(properties, true);
                }

                break;
        }
    }

    private static void SetThinking(AdditionalPropertiesDictionary properties, bool enabled)
    {
        properties[AgentReasoningOptions.EnableThinkingPropertyName] = enabled;
        properties[AgentReasoningOptions.ExtraBodyPropertyName] = new Dictionary<string, object?>
        {
            [AgentReasoningOptions.EnableThinkingPropertyName] = enabled
        };
    }

    private static bool IsThinkingCapable(string provider, string model) =>
        provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("OpenAiCompatible", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("HuggingFace", StringComparison.OrdinalIgnoreCase)
        || model.Contains("qwen", StringComparison.OrdinalIgnoreCase)
        || model.Contains("deepseek", StringComparison.OrdinalIgnoreCase);
}
