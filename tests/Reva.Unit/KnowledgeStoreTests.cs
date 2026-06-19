using Reva.Infrastructure.Knowledge;

namespace Reva.Unit;

public sealed class KnowledgeStoreTests
{
    [Theory]
    [InlineData("bordereau", "bordereaux-explained")]
    [InlineData("reconcile", "reconciliation-method")]
    [InlineData("CRS", "crs-v52")]
    public async Task SearchKnowledgeFindsDomainArticleByKeyword(string query, string expectedSlug)
    {
        var store = new EmbeddedKnowledgeStore();

        var results = await store.SearchAsync(query, 4, CancellationToken.None);

        Assert.Contains(results, result => result.Slug == expectedSlug);
        var match = results.First(result => result.Slug == expectedSlug);
        Assert.False(string.IsNullOrWhiteSpace(match.Title));
        Assert.False(string.IsNullOrWhiteSpace(match.Snippet));
        Assert.True(match.Snippet.Length <= 405);
    }
}
