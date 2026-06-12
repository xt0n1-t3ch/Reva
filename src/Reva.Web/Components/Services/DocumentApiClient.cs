using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reva.Core.Contracts;
using Reva.Core.Documents;

namespace Reva.Web.Components.Services;

public sealed record UploadOutcome(bool Ok, string Message, Guid? Id);

public sealed class DocumentApiClient
{
    private const string Root = "api/documents";
    private const long MaxUploadBytes = DocumentIntakePolicy.MaxFileBytes;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    private static readonly JsonSerializerOptions JsonOptions = BuildOptions();

    private readonly Uri _base;

    public DocumentApiClient(string baseUri)
    {
        _base = new Uri(baseUri.EndsWith('/') ? baseUri : baseUri + "/", UriKind.Absolute);
    }

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private Uri Url(string relative) => new(_base, relative);

    public string ExportUrl(Guid id, string format) => Url($"{Root}/{id}/export?format={format}").ToString();

    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken token = default)
    {
        var result = await Http.GetFromJsonAsync<List<DocumentSummary>>(Url($"{Root}/"), JsonOptions, token);
        return result ?? [];
    }

    public async Task<DocumentDetail?> GetAsync(Guid id, CancellationToken token = default)
    {
        using var response = await Http.GetAsync(Url($"{Root}/{id}"), token);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DocumentDetail>(JsonOptions, token);
    }

    public async Task<UploadOutcome> UploadAsync(string fileName, string contentType, Stream content, CancellationToken token = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "file", fileName);

        using var response = await Http.PostAsync(Url($"{Root}/"), form, token);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<DocumentUploadResult>(JsonOptions, token);
            return new UploadOutcome(true, $"{fileName} uploaded", created?.Id);
        }

        var detail = await ReadErrorAsync(response, token);
        return new UploadOutcome(false, detail, null);
    }

    public async Task<DocumentDetail?> ReviewAsync(Guid id, ReviewDecision decision, CancellationToken token = default)
    {
        using var response = await Http.PostAsJsonAsync(Url($"{Root}/{id}/review"), decision, JsonOptions, token);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DocumentDetail>(JsonOptions, token);
    }

    public static long MaxUpload => MaxUploadBytes;

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken token)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions, token);
            if (!string.IsNullOrWhiteSpace(body?.Error))
            {
                return body.Error;
            }
        }
        catch (JsonException)
        {
        }
        catch (NotSupportedException)
        {
        }

        return $"Upload failed ({(int)response.StatusCode})";
    }

    private sealed record ApiError(string? Error);
}

