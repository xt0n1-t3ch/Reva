using System.Reflection;
using System.Text.Json;

namespace Reva.Infrastructure.Knowledge;

public sealed class EmbeddedKnowledgeStore : IKnowledgeStore
{
    public const string SeedResourceName = "reva.knowledge.seed.json";

    private const int SnippetLength = 400;
    private const int MaxQueryLength = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<IReadOnlyList<KnowledgeArticle>> Articles = new(LoadArticles, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<IReadOnlyList<KnowledgeArticleSummary>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<KnowledgeArticleSummary> summaries = Articles.Value
            .Select(article => new KnowledgeArticleSummary(article.Slug, article.Title, article.Category, article.Summary))
            .ToList();
        return Task.FromResult(summaries);
    }

    public Task<KnowledgeArticle?> GetAsync(string slug, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedSlug = slug.Trim();
        var article = Articles.Value.FirstOrDefault(item => item.Slug.Equals(normalizedSlug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(article);
    }

    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var terms = NormalizeTerms(query);
        if (terms.Count == 0 || maxResults <= 0)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeSearchResult>>([]);
        }

        var results = Articles.Value
            .Select((article, index) => new
            {
                Article = article,
                Index = index,
                Score = terms.Sum(term => CountMatches(SearchText(article), term)),
                MatchIndex = BestMatchIndex(article.Content, article.Summary, article.Title, terms)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .Take(Math.Min(maxResults, 4))
            .Select(item => new KnowledgeSearchResult(
                item.Article.Slug,
                item.Article.Title,
                BuildSnippet(item.Article, terms, item.MatchIndex)))
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgeSearchResult>>(results);
    }

    private static List<KnowledgeArticle> LoadArticles()
    {
        var assembly = typeof(EmbeddedKnowledgeStore).Assembly;
        using var stream = assembly.GetManifestResourceStream(SeedResourceName)
            ?? throw new InvalidOperationException($"Embedded knowledge seed '{SeedResourceName}' was not found.");
        var articles = JsonSerializer.Deserialize<List<KnowledgeArticle>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Embedded knowledge seed did not contain an article array.");
        return articles;
    }

    private static string SearchText(KnowledgeArticle article) =>
        string.Join('\n', article.Title, article.Summary, article.Content);

    private static List<string> NormalizeTerms(string query) =>
        (query.Length > MaxQueryLength ? query[..MaxQueryLength] : query).Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .Take(16)
            .ToList();

    private static int CountMatches(string text, string term)
    {
        var count = 0;
        var index = 0;
        while (index < text.Length)
        {
            var found = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return count;
            }

            count++;
            index = found + term.Length;
        }

        return count;
    }

    private static int BestMatchIndex(string content, string summary, string title, IReadOnlyList<string> terms)
    {
        foreach (var source in new[] { content, summary, title })
        {
            var best = terms
                .Select(term => source.IndexOf(term, StringComparison.OrdinalIgnoreCase))
                .Where(index => index >= 0)
                .DefaultIfEmpty(-1)
                .Min();
            if (best >= 0)
            {
                return best;
            }
        }

        return 0;
    }

    private static string BuildSnippet(KnowledgeArticle article, IReadOnlyList<string> terms, int matchIndex)
    {
        var source = terms.Any(term => article.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? article.Content
            : terms.Any(term => article.Summary.Contains(term, StringComparison.OrdinalIgnoreCase))
                ? article.Summary
                : article.Title;
        var index = terms
            .Select(term => source.IndexOf(term, StringComparison.OrdinalIgnoreCase))
            .Where(candidate => candidate >= 0)
            .DefaultIfEmpty(matchIndex)
            .Min();
        var start = Math.Max(0, index - (SnippetLength / 2));
        var length = Math.Min(SnippetLength, source.Length - start);
        var snippet = source.Substring(start, length).Replace('\n', ' ').Trim();
        if (start > 0)
        {
            snippet = "…" + snippet;
        }

        if (start + length < source.Length)
        {
            snippet += "…";
        }

        return snippet;
    }
}
