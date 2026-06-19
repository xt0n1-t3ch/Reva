namespace Reva.Infrastructure.Knowledge;

public sealed record KnowledgeArticle(
    string Slug,
    string Title,
    string Category,
    string Summary,
    string Content);

public sealed record KnowledgeArticleSummary(
    string Slug,
    string Title,
    string Category,
    string Summary);

public sealed record KnowledgeSearchResult(
    string Slug,
    string Title,
    string Snippet);

public interface IKnowledgeStore
{
    Task<IReadOnlyList<KnowledgeArticleSummary>> ListAsync(CancellationToken cancellationToken);
    Task<KnowledgeArticle?> GetAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken);
}
