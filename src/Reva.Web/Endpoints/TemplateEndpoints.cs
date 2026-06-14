using Reva.Core.Contracts;
using Reva.Infrastructure.Export;

namespace Reva.Web.Endpoints;

public static class TemplateEndpoints
{
    public static RouteGroupBuilder MapTemplateEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/templates").WithTags("Templates");

        group.MapGet("/", async (IExportTemplateStore store, CancellationToken cancellationToken) =>
            Results.Ok(await store.ListAsync(cancellationToken)));

        group.MapGet("/{id:guid}", async (Guid id, IExportTemplateStore store, CancellationToken cancellationToken) =>
        {
            var template = await store.GetAsync(id, cancellationToken);
            return template is null ? Results.NotFound() : Results.Ok(template);
        });

        group.MapPost("/", async (ExportTemplateDraft draft, IExportTemplateStore store, CancellationToken cancellationToken) =>
        {
            var created = await store.CreateAsync(draft, cancellationToken);
            return Results.Created($"/api/templates/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (Guid id, ExportTemplateDraft draft, IExportTemplateStore store, CancellationToken cancellationToken) =>
        {
            var updated = await store.UpdateAsync(id, draft, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapPost("/{id:guid}/duplicate", async (Guid id, IExportTemplateStore store, CancellationToken cancellationToken) =>
        {
            var copy = await store.DuplicateAsync(id, cancellationToken);
            return copy is null ? Results.NotFound() : Results.Created($"/api/templates/{copy.Id}", copy);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IExportTemplateStore store, CancellationToken cancellationToken) =>
        {
            var deleted = await store.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }
}
