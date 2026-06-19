using System.ClientModel;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI;
using Reva.Core.Settings;

namespace Reva.Infrastructure.Agent;

public sealed record LlmChatClientRequest(
    string Provider,
    string BaseUrl,
    string? ApiKey,
    string Model,
    int MaxSteps,
    int NumCtx);

public interface ILlmChatClientFactory
{
    IChatClient Create(LlmChatClientRequest request);
}

public sealed class LlmChatClientFactory : ILlmChatClientFactory
{
    public const string ApiKeyPlaceholder = "ollama";
    public const string OpenAiCompatibleSuffix = "/v1";

    private readonly ConcurrentDictionary<LlmChatClientCacheKey, IChatClient> clients = new();

    public IChatClient Create(LlmChatClientRequest request)
    {
        var key = LlmChatClientCacheKey.From(request);
        return clients.GetOrAdd(key, static cacheKey => new OpenAI.Chat.ChatClient(
                cacheKey.Model,
                new ApiKeyCredential(string.IsNullOrEmpty(cacheKey.ApiKey) ? ApiKeyPlaceholder : cacheKey.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(cacheKey.BaseUrl) })
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(configure: client => client.MaximumIterationsPerRequest = cacheKey.MaxSteps)
            .Build());
    }

    public static string NormalizeOpenAiBaseUrl(string provider, string? baseUrl)
    {
        var normalizedProvider = AiProviderNames.Normalize(provider);
        var normalized = AiSettingsDefaults.NormalizeBaseUrl(normalizedProvider, baseUrl);
        return normalized.EndsWith(OpenAiCompatibleSuffix, StringComparison.OrdinalIgnoreCase)
            ? normalized
            : normalized.TrimEnd('/') + OpenAiCompatibleSuffix;
    }

    public static string RemoveOpenAiSuffix(string baseUrl)
    {
        var trimmed = baseUrl.Trim().TrimEnd('/');
        return trimmed.EndsWith(OpenAiCompatibleSuffix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^OpenAiCompatibleSuffix.Length]
            : trimmed;
    }

    private sealed record LlmChatClientCacheKey(
        string Provider,
        string BaseUrl,
        string ApiKey,
        string Model,
        int MaxSteps,
        int NumCtx)
    {
        public static LlmChatClientCacheKey From(LlmChatClientRequest request)
        {
            var provider = AiProviderNames.Normalize(request.Provider);
            return new LlmChatClientCacheKey(
                provider,
                NormalizeOpenAiBaseUrl(provider, request.BaseUrl),
                request.ApiKey ?? string.Empty,
                AiSettingsDefaults.NormalizeModel(request.Model),
                request.MaxSteps,
                request.NumCtx);
        }
    }
}
