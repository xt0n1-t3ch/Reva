using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Reva.Infrastructure.Extraction;

namespace Reva.Ai;

public static class AddRevaAiExtensions
{
    public static IServiceCollection AddRevaAi(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = BuildOptions(configuration);

        services.Configure<AiProcessingOptions>(options =>
        {
            options.BaseUrl = aiOptions.BaseUrl;
            options.OpenAiBaseUrl = aiOptions.OpenAiBaseUrl;
            options.ActiveModel = aiOptions.ActiveModel;
            options.UseVisionExtraction = aiOptions.UseVisionExtraction;
            options.TimeoutSeconds = aiOptions.TimeoutSeconds;
        });

        services.AddHttpClient();
        services.AddSingleton<IModelRegistry, ModelRegistry>();

        if (aiOptions.UseVisionExtraction)
        {
            services.AddSingleton<ILlmFieldExtractor>(_ => new VlmFieldExtractor(
                CreateVisionChatClient(aiOptions),
                Microsoft.Extensions.Options.Options.Create(aiOptions)));
        }

        return services;
    }

    private static AiProcessingOptions BuildOptions(IConfiguration configuration)
    {
        var baseUrl = configuration[AiProcessingOptions.BaseUrlKey] ?? AiProcessingOptions.DefaultBaseUrl;
        return new AiProcessingOptions
        {
            BaseUrl = baseUrl,
            OpenAiBaseUrl = configuration[AiProcessingOptions.OpenAiBaseUrlKey] ?? DeriveOpenAiBaseUrl(baseUrl),
            ActiveModel = configuration[AiProcessingOptions.ActiveModelKey] ?? AiProcessingOptions.DefaultActiveModel,
            UseVisionExtraction = bool.TryParse(configuration[AiProcessingOptions.UseVisionKey], out var useVision) && useVision,
            TimeoutSeconds = int.TryParse(configuration[AiProcessingOptions.TimeoutSecondsKey], out var timeoutSeconds) && timeoutSeconds > 0
                ? timeoutSeconds
                : AiProcessingOptions.DefaultTimeoutSeconds
        };
    }

    private static string DeriveOpenAiBaseUrl(string baseUrl)
    {
        var trimmed = string.IsNullOrWhiteSpace(baseUrl) ? AiProcessingOptions.DefaultBaseUrl : baseUrl.Trim();
        return trimmed.EndsWith(OllamaApi.OpenAiCompatibleSuffix, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : trimmed.TrimEnd('/') + OllamaApi.OpenAiCompatibleSuffix;
    }

    private static IChatClient CreateVisionChatClient(AiProcessingOptions options)
    {
        var endpoint = new Uri(DeriveOpenAiBaseUrl(string.IsNullOrWhiteSpace(options.OpenAiBaseUrl) ? options.BaseUrl : options.OpenAiBaseUrl));
        var model = string.IsNullOrWhiteSpace(options.ActiveModel) ? AiProcessingOptions.DefaultActiveModel : options.ActiveModel.Trim();
        return new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(OllamaApi.ApiKeyPlaceholder), new OpenAIClientOptions { Endpoint = endpoint }).AsIChatClient();
    }
}
