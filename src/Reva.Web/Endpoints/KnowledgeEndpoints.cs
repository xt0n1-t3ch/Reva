using Reva.Infrastructure.Knowledge;

namespace Reva.Web.Endpoints;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/knowledge", async (IKnowledgeStore store, CancellationToken cancellationToken) =>
        {
            var articles = await store.ListAsync(cancellationToken);
            return Results.Ok(articles);
        }).WithTags("Knowledge");

        routes.MapGet("/api/knowledge/{slug}", async (string slug, IKnowledgeStore store, CancellationToken cancellationToken) =>
        {
            var article = await store.GetAsync(slug, cancellationToken);
            return article is null ? (IResult)Results.NotFound() : Results.Ok(article);
        }).WithTags("Knowledge");

        return routes;
    }
}
