using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reva.Core.Settings;

namespace Reva.Infrastructure.Agent;

public sealed record ModelDiscoveryRequest(string? Provider, string? BaseUrl, string? ApiKey);

public sealed record DiscoveredModel(string Id, string Label);

public sealed record ModelDiscoveryResponse(IReadOnlyList<DiscoveredModel> Models, string Source, string? Message = null);

public interface ILlmModelDiscoveryService
{
    Task<ModelDiscoveryResponse> DiscoverAsync(ModelDiscoveryRequest request, CancellationToken cancellationToken);
}

public sealed class LlmModelDiscoveryService(IHttpClientFactory httpClientFactory) : ILlmModelDiscoveryService
{
    public const string SourceEndpoint = "endpoint";
    public const string SourceCurated = "curated";
    public const string SourceError = "error";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<DiscoveredModel> CuratedModels =
    [
        new(AiSettingsDefaults.DefaultModel, AiSettingsDefaults.DefaultModel),
        new("llama3.2-vision:11b", "llama3.2-vision:11b"),
        new("granite3.2-vision:2b", "granite3.2-vision:2b"),
        new("minicpm-v:8b", "minicpm-v:8b"),
        new("llama3.1:8b", "llama3.1:8b")
    ];

    public async Task<ModelDiscoveryResponse> DiscoverAsync(ModelDiscoveryRequest request, CancellationToken cancellationToken)
    {
        var provider = AiProviderNames.Normalize(request.Provider);
        var baseUrl = LlmChatClientFactory.NormalizeOpenAiBaseUrl(provider, request.BaseUrl);
        try
        {
            var models = provider == AiProviderNames.Ollama
                ? await DiscoverOllamaAsync(baseUrl, cancellationToken).ConfigureAwait(false)
                : await DiscoverOpenAiModelsAsync(baseUrl, request.ApiKey, cancellationToken).ConfigureAwait(false);

            if (models.Count > 0)
            {
                return new ModelDiscoveryResponse(models, SourceEndpoint);
            }

            return Curated("The endpoint returned no models.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested
            && ex is HttpRequestException
                or TaskCanceledException
                or JsonException
                or NotSupportedException
                or UriFormatException
                or InvalidOperationException)
        {
            return Curated($"Model discovery failed: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<DiscoveredModel>> DiscoverOllamaAsync(string baseUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(LlmChatClientFactory.RemoveOpenAiSuffix(baseUrl).TrimEnd('/') + "/"), "api/tags"));
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        return payload?.Models is null
            ? []
            : payload.Models
                .Select(model => model.Name)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => new DiscoveredModel(id, id))
                .ToList();
    }

    private async Task<IReadOnlyList<DiscoveredModel>> DiscoverOpenAiModelsAsync(string baseUrl, string? apiKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "models"));
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        return payload?.Data is null
            ? []
            : payload.Data
                .Select(model => model.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => new DiscoveredModel(id, id))
                .ToList();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        var client = httpClientFactory.CreateClient(nameof(LlmModelDiscoveryService));
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
    }

    private static ModelDiscoveryResponse Curated(string message) =>
        CuratedModels.Count > 0
            ? new ModelDiscoveryResponse(CuratedModels, SourceCurated, message)
            : new ModelDiscoveryResponse([], SourceError, message);

    private sealed record OllamaTagsResponse([property: JsonPropertyName("models")] IReadOnlyList<OllamaModelTag>? Models);

    private sealed record OllamaModelTag([property: JsonPropertyName("name")] string? Name);

    private sealed record OpenAiModelsResponse([property: JsonPropertyName("data")] IReadOnlyList<OpenAiModel>? Data);

    private sealed record OpenAiModel([property: JsonPropertyName("id")] string? Id);
}
