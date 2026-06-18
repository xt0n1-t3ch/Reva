using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reva.Ai;
using Reva.Ai.Models;

namespace Reva.Unit;

public sealed class ModelRegistryTests
{
    [Fact]
    public async Task IsOllamaAvailableAsyncReturnsFalseWhenHttpFails()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions()));

        var available = await registry.IsOllamaAvailableAsync(CancellationToken.None);

        Assert.False(available);
    }

    [Fact]
    public async Task ListAsyncReturnsCuratedModelsWhenOllamaAbsent()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions()));

        var models = await registry.ListAsync(CancellationToken.None);

        Assert.Equal(CuratedModels.Menu.Count, models.Count);
        Assert.All(models, m => Assert.False(m.Installed));
    }

    [Fact]
    public async Task ListAsyncMergesCuratedModelsWithInstalledOnes()
    {
        var json = JsonSerializer.Serialize(new
        {
            models = new[]
            {
                new { name = "qwen3-vl:8b" },
                new { name = "custom-local-model" }
            }
        });

        using var registry = new ModelRegistry(
            new FixedResponseHttpClientFactory(HttpStatusCode.OK, json),
            Options.Create(new AiProcessingOptions()));

        var models = await registry.ListAsync(CancellationToken.None);

        var qwen = models.FirstOrDefault(m => m.Id == "qwen3-vl:8b");
        Assert.NotNull(qwen);
        Assert.True(qwen.Installed);

        var llama = models.FirstOrDefault(m => m.Id == "llama4");
        Assert.NotNull(llama);
        Assert.False(llama.Installed);

        var custom = models.FirstOrDefault(m => m.Id == "custom-local-model");
        Assert.NotNull(custom);
        Assert.True(custom.Installed);
    }

    [Fact]
    public async Task ListAsyncDoesNotDuplicateCuratedModelsWhenAllInstalled()
    {
        var installedNames = CuratedModels.Menu.Select(m => new { name = m.Id }).ToArray();
        var json = JsonSerializer.Serialize(new { models = installedNames });

        using var registry = new ModelRegistry(
            new FixedResponseHttpClientFactory(HttpStatusCode.OK, json),
            Options.Create(new AiProcessingOptions()));

        var models = await registry.ListAsync(CancellationToken.None);

        var ids = models.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct(StringComparer.OrdinalIgnoreCase).Count(), ids.Count);
    }

    [Fact]
    public async Task GetActiveModelAsyncReturnsValueSetInMemory()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions()));

        await registry.SetActiveModelAsync("seeded-model", CancellationToken.None);
        var active = await registry.GetActiveModelAsync(CancellationToken.None);

        Assert.Equal("seeded-model", active);
    }

    [Fact]
    public async Task GetActiveModelAsyncReturnsDefaultActiveModelConstantWhenOptionsBlank()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions { ActiveModel = string.Empty }));

        await registry.SetActiveModelAsync(AiProcessingOptions.DefaultActiveModel, CancellationToken.None);
        var active = await registry.GetActiveModelAsync(CancellationToken.None);

        Assert.Equal(AiProcessingOptions.DefaultActiveModel, active);
    }

    [Fact]
    public async Task SetActiveModelAsyncPersistsAndGetReturnsIt()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions()));

        await registry.SetActiveModelAsync("my-custom-model", CancellationToken.None);
        var active = await registry.GetActiveModelAsync(CancellationToken.None);

        Assert.Equal("my-custom-model", active);
    }

    [Fact]
    public async Task SetActiveModelAsyncIgnoresBlankModelId()
    {
        using var registry = new ModelRegistry(
            new FailingHttpClientFactory(),
            Options.Create(new AiProcessingOptions()));

        await registry.SetActiveModelAsync("established-model", CancellationToken.None);
        await registry.SetActiveModelAsync(string.Empty, CancellationToken.None);
        var active = await registry.GetActiveModelAsync(CancellationToken.None);

        Assert.Equal("established-model", active);
    }

    [Fact]
    public async Task IsOllamaAvailableAsyncReturnsFalseWhenHttpReturnsNonSuccess()
    {
        using var registry = new ModelRegistry(
            new FixedResponseHttpClientFactory(HttpStatusCode.ServiceUnavailable, string.Empty),
            Options.Create(new AiProcessingOptions()));

        var available = await registry.IsOllamaAvailableAsync(CancellationToken.None);

        Assert.False(available);
    }

    [Fact]
    public async Task IsOllamaAvailableAsyncReturnsTrueWhenHttpSucceeds()
    {
        var json = JsonSerializer.Serialize(new { models = Array.Empty<object>() });

        using var registry = new ModelRegistry(
            new FixedResponseHttpClientFactory(HttpStatusCode.OK, json),
            Options.Create(new AiProcessingOptions()));

        var available = await registry.IsOllamaAvailableAsync(CancellationToken.None);

        Assert.True(available);
    }

    private sealed class FailingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FailingHandler());

        private sealed class FailingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromException<HttpResponseMessage>(new HttpRequestException("Ollama not reachable"));
        }
    }

    private sealed class FixedResponseHttpClientFactory(HttpStatusCode statusCode, string responseBody) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FixedHandler(statusCode, responseBody));

        private sealed class FixedHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
}
