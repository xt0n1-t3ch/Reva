using Reva.Core.Settings;
using Reva.Infrastructure.Agent;

namespace Reva.Unit;

public sealed class LlmChatClientFactoryTests
{
    [Fact]
    public void CreateCachesClientsByNormalizedProviderTuple()
    {
        var factory = new LlmChatClientFactory();

        var first = factory.Create(new LlmChatClientRequest(
            AiProviderNames.Ollama,
            "http://localhost:11434",
            null,
            AiSettingsDefaults.DefaultModel,
            AgentChatOptions.DefaultMaxSteps,
            AgentChatOptions.DefaultNumCtx));
        var second = factory.Create(new LlmChatClientRequest(
            "ollama",
            AiSettingsDefaults.OllamaBaseUrl,
            null,
            AiSettingsDefaults.DefaultModel,
            AgentChatOptions.DefaultMaxSteps,
            AgentChatOptions.DefaultNumCtx));

        Assert.Same(first, second);
    }

    [Fact]
    public void CreateBuildsDistinctClientsForDistinctProviderTuples()
    {
        var factory = new LlmChatClientFactory();

        var ollama = factory.Create(new LlmChatClientRequest(
            AiProviderNames.Ollama,
            AiSettingsDefaults.OllamaBaseUrl,
            null,
            AiSettingsDefaults.DefaultModel,
            AgentChatOptions.DefaultMaxSteps,
            AgentChatOptions.DefaultNumCtx));
        var compatible = factory.Create(new LlmChatClientRequest(
            AiProviderNames.OpenAiCompatible,
            AiSettingsDefaults.OpenAiCompatibleBaseUrl,
            "key",
            AiSettingsDefaults.DefaultModel,
            AgentChatOptions.DefaultMaxSteps,
            AgentChatOptions.DefaultNumCtx));
        var differentModel = factory.Create(new LlmChatClientRequest(
            AiProviderNames.Ollama,
            AiSettingsDefaults.OllamaBaseUrl,
            null,
            "another-model",
            AgentChatOptions.DefaultMaxSteps,
            AgentChatOptions.DefaultNumCtx));

        Assert.NotSame(ollama, compatible);
        Assert.NotSame(ollama, differentModel);
    }
}
