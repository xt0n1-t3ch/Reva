using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Infrastructure;
using Reva.Infrastructure.Ingestion;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.Web.Endpoints;

public static class ApiSurfaceEndpoints
{
    public static IEndpointRouteBuilder MapApiSurfaceEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/schema-mappings", async (RevaDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var mappings = await dbContext.LearnedSchemaMappings.AsNoTracking()
                .OrderBy(mapping => mapping.SenderKey)
                .ThenBy(mapping => mapping.SourceHeader)
                .ToListAsync(cancellationToken);
            return Results.Ok(mappings);
        }).WithTags("Schema mappings");

        routes.MapGet("/api/settings", async (ISettingsStore settings, CancellationToken cancellationToken) => Results.Ok(await settings.GetAsync(cancellationToken))).WithTags("Settings");

        routes.MapPut("/api/settings", async (Reva.Core.Settings.AppSettings draft, ISettingsStore settings, CancellationToken cancellationToken) => Results.Ok(await settings.SaveAsync(draft, cancellationToken))).WithTags("Settings");

        var data = routes.MapGroup("/api/data").WithTags("Data management");
        data.MapPost("/reseed", async (IDataMaintenance maintenance, CancellationToken cancellationToken) =>
        {
            var seeded = await maintenance.ReseedDemoAsync(cancellationToken);
            return Results.Ok(new { seeded });
        }).DisableAntiforgery();

        data.MapPost("/clear", async (IDataMaintenance maintenance, CancellationToken cancellationToken) =>
        {
            var deleted = await maintenance.ClearAllDocumentsAsync(cancellationToken);
            return Results.Ok(new { deleted });
        }).DisableAntiforgery();

        routes.MapGet("/api/inbound-sources", (IInboundSourceRegistry registry) => Results.Ok(registry.Statuses())).WithTags("Inbound sources");

        routes.MapGet("/api/reconciliation/{id:guid}", async (Guid id, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken) =>
        {
            var document = await LoadDocumentAsync(dbContext, id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(assembler.Assemble(document).Reconciliation);
        }).WithTags("Reconciliation");

        var tools = routes.MapGroup("/api/agent-tools").WithTags("Agent tools");
        tools.MapPost("/ingest", async (IFormFile file, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            return Results.Ok(await workflow.IngestAsync(file.FileName, file.ContentType, stream, cancellationToken));
        }).DisableAntiforgery();

        tools.MapGet("/get-review-payload/{id:guid}", async (Guid id, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken) =>
        {
            var document = await LoadDocumentAsync(dbContext, id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(assembler.Assemble(document));
        });

        tools.MapPost("/run-extraction/{id:guid}", async (Guid id, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            var detail = await workflow.GetAsync(id, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        tools.MapGet("/reconcile/{id:guid}", async (Guid id, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken) =>
        {
            var document = await LoadDocumentAsync(dbContext, id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(assembler.Assemble(document).Reconciliation);
        });

        tools.MapGet("/export/{id:guid}", async (Guid id, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            var export = await workflow.ExportAsync(id, cancellationToken);
            return export is null ? Results.NotFound() : Results.Ok(export);
        });

        tools.MapGet("/explain-field/{id:guid}/{field}", async (Guid id, string field, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken) =>
        {
            var document = await LoadDocumentAsync(dbContext, id, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }

            var payload = assembler.Assemble(document);
            var value = payload.Fields.FirstOrDefault(item => item.Key.Equals(field, StringComparison.OrdinalIgnoreCase));
            return value is null ? Results.NotFound() : Results.Ok(new { field = value, citations = value.Provenance.Citations });
        });

        return routes;
    }

    private static Task<DocumentRecord?> LoadDocumentAsync(RevaDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.AsSplitQuery()
            .Include(item => item.Fields)
            .Include(item => item.Tables)
            .Include(item => item.Exceptions)
            .Include(item => item.SourceSpans)
            .Include(item => item.Pages)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
}
