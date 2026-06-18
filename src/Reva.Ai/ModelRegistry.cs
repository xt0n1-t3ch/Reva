using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Reva.Ai.Models;

namespace Reva.Ai;

public sealed class ModelRegistry(IHttpClientFactory httpClientFactory, IOptions<AiProcessingOptions> options) : IModelRegistry, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private string? _activeModel;

    public void Dispose() => _stateGate.Dispose();

    public async Task<IReadOnlyList<ModelDescriptor>> ListAsync(CancellationToken ct)
    {
        var installed = await ProbeInstalledAsync(ct);
        var installedSet = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);

        var merged = CuratedModels.Menu
            .Select(model => model with { Installed = installedSet.Contains(model.Id) })
            .ToList();

        var curatedIds = new HashSet<string>(CuratedModels.Menu.Select(model => model.Id), StringComparer.OrdinalIgnoreCase);
        merged.AddRange(installed
            .Where(id => !curatedIds.Contains(id))
            .Select(id => new ModelDescriptor(id, id, ModelKinds.Text, string.Empty, true)));

        return merged;
    }

    public async Task<bool> IsOllamaAvailableAsync(CancellationToken ct)
    {
        try
        {
            using var response = await SendTagsRequestAsync(ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<string?> GetActiveModelAsync(CancellationToken ct)
    {
        await _stateGate.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_activeModel))
            {
                return _activeModel;
            }

            _activeModel = ReadPersistedModel() ?? FallbackModel();
            return _activeModel;
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task SetActiveModelAsync(string modelId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        var normalized = modelId.Trim();
        await _stateGate.WaitAsync(ct);
        try
        {
            _activeModel = normalized;
            PersistModel(normalized);
        }
        finally
        {
            _stateGate.Release();
        }
    }

    private async Task<IReadOnlyList<string>> ProbeInstalledAsync(CancellationToken ct)
    {
        try
        {
            using var response = await SendTagsRequestAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(SerializerOptions, ct);
            return payload?.Models is null
                ? []
                : payload.Models
                    .Select(model => model.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException && !ct.IsCancellationRequested)
        {
            return [];
        }
    }

    private Task<HttpResponseMessage> SendTagsRequestAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(ModelRegistry));
        var requestUri = new Uri(new Uri(NormalizeBaseUrl()), OllamaApi.TagsPath);
        return client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private string NormalizeBaseUrl()
    {
        var configured = options.Value.BaseUrl;
        var baseUrl = string.IsNullOrWhiteSpace(configured) ? AiProcessingOptions.DefaultBaseUrl : configured.Trim();
        if (baseUrl.EndsWith(OllamaApi.OpenAiCompatibleSuffix, StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl[..^OllamaApi.OpenAiCompatibleSuffix.Length];
        }

        return baseUrl.TrimEnd('/') + "/";
    }

    private string FallbackModel()
    {
        var configured = options.Value.ActiveModel;
        return string.IsNullOrWhiteSpace(configured) ? AiProcessingOptions.DefaultActiveModel : configured.Trim();
    }

    private static string? ReadPersistedModel()
    {
        var path = AiStatePaths.ModelStateFilePath();
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var state = JsonSerializer.Deserialize<ModelState>(File.ReadAllText(path), SerializerOptions);
            return string.IsNullOrWhiteSpace(state?.ActiveModel) ? null : state.ActiveModel.Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static void PersistModel(string modelId)
    {
        var path = AiStatePaths.ModelStateFilePath();
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(new ModelState(modelId), SerializerOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed record ModelState(string ActiveModel);

    private sealed record OllamaTagsResponse([property: JsonPropertyName("models")] IReadOnlyList<OllamaModelTag>? Models);

    private sealed record OllamaModelTag([property: JsonPropertyName("name")] string? Name);
}
