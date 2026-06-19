using System.Net;
using System.Text.Json;

namespace Reva.Integration;

public sealed class KnowledgeEndpointsTests(RevaWebApplicationFactory factory) : IClassFixture<RevaWebApplicationFactory>
{
    [Fact]
    public async Task ListReturnsSummariesWithoutContentInSeedOrder()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/knowledge");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var articles = document.RootElement;

        response.EnsureSuccessStatusCode();
        Assert.True(articles.GetArrayLength() > 0);
        var first = articles[0];
        Assert.Equal("getting-started", first.GetProperty("slug").GetString());
        Assert.Equal("Getting started with Reva", first.GetProperty("title").GetString());
        Assert.Equal("Guide", first.GetProperty("category").GetString());
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("summary").GetString()));
        Assert.False(first.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task GetReturnsFullArticleWithContent()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/knowledge/reconciliation-method");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var article = document.RootElement;

        response.EnsureSuccessStatusCode();
        Assert.Equal("reconciliation-method", article.GetProperty("slug").GetString());
        Assert.Equal("How Reva reconciles control totals", article.GetProperty("title").GetString());
        Assert.Equal("Methodology", article.GetProperty("category").GetString());
        Assert.Contains("expected", article.GetProperty("content").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMissingArticleReturns404()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/knowledge/missing-article");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
