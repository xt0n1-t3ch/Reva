using System.Text;
using Reva.Core.Contracts;
using Reva.Infrastructure;
using Reva.Infrastructure.Export;

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

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = Encoding.UTF8.GetBytes(ToCsv(export));
                return Results.File(bytes, "text/csv", $"reva-export-{id:N}.csv");
            }

            return Results.Ok(export);
        });

        return group;
    }

    private static string ToCsv(ExportRecord export)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Name,Value");
        foreach (var field in export.Fields)
        {
            builder.Append(EscapeCsv(field.Key));
            builder.Append(',');
            builder.AppendLine(EscapeCsv(field.Value));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
