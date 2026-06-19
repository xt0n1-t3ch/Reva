using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Infrastructure;
using Reva.Infrastructure.Export;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Rendering;
using Reva.Infrastructure.Review;

namespace Reva.Web.Endpoints;

public static class DocumentEndpoints
{
    public static RouteGroupBuilder MapDocumentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/", async (IFormFile file, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var result = await workflow.IngestAsync(file.FileName, file.ContentType, stream, cancellationToken);
                return Results.Created($"/api/documents/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).DisableAntiforgery();

        group.MapGet("/", async (IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            var documents = await workflow.ListAsync(cancellationToken);
            return Results.Ok(documents);
        });

        group.MapGet("/{id:guid}", async (Guid id, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            var document = await workflow.GetAsync(id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        group.MapPost("/{id:guid}/review", async (Guid id, ReviewDecision decision, IDocumentWorkflow workflow, CancellationToken cancellationToken) =>
        {
            var document = await workflow.ReviewAsync(id, decision, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });


        group.MapGet("/{id:guid}/review-payload", async (Guid id, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken) =>
        {
            var document = await dbContext.Documents
                .AsSplitQuery()
                .Include(item => item.Fields)
                .Include(item => item.Tables)
                .Include(item => item.Exceptions)
                .Include(item => item.SourceSpans)
                .Include(item => item.Pages)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(assembler.Assemble(document));
        });

        group.MapGet("/{id:guid}/process-stream", async (Guid id, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, HttpContext http) =>
        {
            var document = await dbContext.Documents
                .AsSplitQuery()
                .Include(item => item.Fields)
                .Include(item => item.Tables)
                .Include(item => item.Exceptions)
                .Include(item => item.SourceSpans)
                .Include(item => item.Pages)
                .FirstOrDefaultAsync(item => item.Id == id, http.RequestAborted);
            if (document is null)
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var payload = assembler.Assemble(document);
            http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            http.Response.Headers.ContentType = "text/event-stream; charset=utf-8";
            http.Response.Headers.CacheControl = "no-cache, no-transform";
            http.Response.Headers["X-Accel-Buffering"] = "no";

            await DocumentProcessingStream.WriteAsync(http.Response, document.FileName, payload, http.RequestAborted);
        });

        group.MapGet("/{id:guid}/pages/{page:int}.png", async (Guid id, int page, RevaDbContext dbContext, IPdfPageImageRenderer renderer, CancellationToken cancellationToken) =>
        {
            var document = await dbContext.Documents
                .Include(item => item.Pages)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (document is null || page < 1)
            {
                return Results.NotFound();
            }

            var existing = document.Pages.FirstOrDefault(item => item.Page == page);
            var path = existing?.ImagePath;
            if (string.IsNullOrWhiteSpace(path) && string.Equals(document.Extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var image = await renderer.RenderPageAsync(document.StoragePath, page, Path.Combine(Path.GetTempPath(), "reva-page-cache", document.Id.ToString("N")), cancellationToken);
                path = image.ImagePath;
            }

            return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                ? Results.File(path, "image/png")
                : Results.NotFound();
        });

        group.MapGet("/{id:guid}/export", async (Guid id, string? format, Guid? templateId, IDocumentWorkflow workflow, IExportTemplateStore templates, IDocumentExporter exporter, CancellationToken cancellationToken) =>
        {
            // Raw export: the parsed source text/markdown for any document, even ones with no
            // canonical fields. JSON/CSV below are the canonical reinsurance-field export.
            if (string.Equals(format, "raw", StringComparison.OrdinalIgnoreCase))
            {
                var detail = await workflow.GetAsync(id, cancellationToken);
                return detail is null
                    ? Results.NotFound()
                    : Results.Text(detail.ParsedMarkdown ?? string.Empty, "text/plain; charset=utf-8");
            }

            // Templated export: apply a saved export template (custom columns/labels/format).
            if (templateId is Guid layoutId)
            {
                var detail = await workflow.GetAsync(id, cancellationToken);
                if (detail is null)
                {
                    return Results.NotFound();
                }

                var template = await templates.GetAsync(layoutId, cancellationToken);
                if (template is null)
                {
                    return Results.NotFound(new { error = "Export template not found." });
                }

                var file = exporter.Export(detail, template);
                return Results.File(file.Content, file.ContentType, file.FileName);
            }

            if (TryResolveExportFormat(format, out var exportFormat))
            {
                var detail = await workflow.GetAsync(id, cancellationToken);
                if (detail is null)
                {
                    return Results.NotFound();
                }

                var template = ExportTemplateDefaults.All.First(candidate => candidate.Format == exportFormat);
                var file = exporter.Export(detail, template);
                return Results.File(file.Content, file.ContentType, file.FileName);
            }

            ExportRecord? export;
            try
            {
                export = await workflow.ExportAsync(id, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }

            if (export is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(export);
        });

        return group;
    }

    private static bool TryResolveExportFormat(string? format, out ExportFormat exportFormat)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            exportFormat = ExportFormat.Csv;
            return false;
        }

        if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
        {
            exportFormat = ExportFormat.Excel;
            return true;
        }

        return Enum.TryParse(format, ignoreCase: true, out exportFormat);
    }
}
