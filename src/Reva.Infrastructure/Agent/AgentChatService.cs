using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.Infrastructure.Agent;

public interface IAgentChatService
{
    IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, CancellationToken cancellationToken);
    IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken, IDataMaintenance? maintenance = null);
}

public sealed class AgentChatService(IChatClient chatClient, IOptions<AgentChatOptions> options, IAppActionBus actionBus) : IAgentChatService
{
    private static readonly JsonSerializerOptions ToolJsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };

    public IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, CancellationToken cancellationToken) =>
        chatClient.GetStreamingResponseAsync(
            messages,
            new ChatOptions
            {
                Tools = [.. tools],
                Temperature = (float)options.Value.Temperature,
                ModelId = options.Value.Model,
                AdditionalProperties = new AdditionalPropertiesDictionary { [AgentChatOptions.NumCtxPropertyName] = options.Value.NumCtx }
            },
            cancellationToken);

    public IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken, IDataMaintenance? maintenance = null)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(assembler);

        Task<object> ListDocumentsAsync() => CatchAsync(async () =>
        {
            var documents = await workflow.ListAsync(cancellationToken);
            return new
            {
                count = documents.Count,
                documents = documents.Select(document => new
                {
                    id = document.Id,
                    fileName = document.FileName,
                    status = document.Status,
                    type = document.DocumentType,
                    confidence = Round(document.Confidence),
                    exceptions = document.ExceptionCount,
                    reviewState = document.ReviewState
                }).ToList()
            };
        });

        Task<object> GetDocumentAsync(string documentId) => CatchAsync(async () =>
        {
            var detail = await workflow.GetAsync(Guid.Parse(documentId), cancellationToken);
            if (detail is null)
            {
                return new { error = "Document not found" };
            }

            return new
            {
                id = detail.Id,
                fileName = detail.FileName,
                type = detail.DocumentType,
                status = detail.Status,
                confidence = Round(detail.Confidence),
                fields = detail.Fields.Select(field => new
                {
                    name = field.Name,
                    value = field.Value,
                    confidence = Round(field.Confidence)
                }).ToList(),
                exceptions = detail.Exceptions.Select(issue => new
                {
                    severity = issue.Severity,
                    message = issue.Message,
                    field = issue.FieldName
                }).ToList()
            };
        });

        Task<object> ReconcileAsync(string documentId) => CatchAsync(async () =>
        {
            var record = await LoadDocumentAsync(dbContext, Guid.Parse(documentId), cancellationToken);
            if (record is null)
            {
                return new { error = "Document not found" };
            }

            var checks = assembler.Assemble(record).Reconciliation;
            return new
            {
                count = checks.Count,
                checks = checks.Select(check => new
                {
                    name = check.Name,
                    detected = check.Detected.Value,
                    expected = check.Expected.Value,
                    delta = check.Delta,
                    status = check.Status,
                    explanation = check.Explanation
                }).ToList()
            };
        });

        Task<object> ExplainFieldAsync(string documentId, string field) => CatchAsync(async () =>
        {
            var record = await LoadDocumentAsync(dbContext, Guid.Parse(documentId), cancellationToken);
            if (record is null)
            {
                return new { error = "Document not found" };
            }

            var payload = assembler.Assemble(record);
            var match = payload.Fields.FirstOrDefault(item =>
                item.Key.Equals(field, StringComparison.OrdinalIgnoreCase)
                || item.Label.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                return new { found = false, available = payload.Fields.Select(item => item.Key).ToList() };
            }

            return new
            {
                found = true,
                key = match.Key,
                label = match.Label,
                value = match.Value,
                confidence = Round(match.Confidence),
                method = match.Provenance.Method,
                citations = match.Provenance.Citations.Select(citation => new
                {
                    page = citation.Page,
                    quote = citation.Quote,
                    role = citation.Role
                }).ToList()
            };
        });

        Task<object> GotoAsync(string route) => CatchAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(route) || !AgentRoutes.All.Contains(route.Trim()))
            {
                return Task.FromResult(Failure(AgentToolMessages.UnknownRoute));
            }

            var normalized = route.Trim().ToLowerInvariant();
            actionBus.Publish(new AppAction(AppActionKind.Navigate, Route: normalized));
            return Task.FromResult(Success($"Navigated to {normalized}."));
        });

        Task<object> OpenDocumentAsync(string documentId) => CatchAsync(async () =>
        {
            if (!TryParseId(documentId, out var id))
            {
                return Failure(AgentToolMessages.InvalidDocumentId);
            }

            var detail = await workflow.GetAsync(id, cancellationToken);
            if (detail is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.OpenDocument, DocumentId: id.ToString()));
            actionBus.Publish(new AppAction(AppActionKind.Navigate, Route: AgentRoutes.Review));
            return Success($"Opened document {detail.FileName}.");
        });

        Task<object> ProcessDocumentsAsync() => CatchAsync(async () =>
        {
            actionBus.Publish(new AppAction(AppActionKind.Toast, Message: AgentToolMessages.Processing));
            var documents = await workflow.ListAsync(cancellationToken);
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.QueueState, documents.Count), new { count = documents.Count });
        });

        Task<object> CorrectFieldAsync(string documentId, string field, string value) => CatchAsync(async () =>
        {
            if (!TryParseId(documentId, out var id))
            {
                return Failure(AgentToolMessages.InvalidDocumentId);
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return Failure(AgentToolMessages.FieldRequired);
            }

            var decision = new ReviewDecision(
                AgentReviewDecisions.NeedsCorrection,
                AgentReviewDecisions.Reviewer,
                null,
                [new FieldCorrection(field.Trim(), value ?? string.Empty)]);
            var result = await workflow.ReviewAsync(id, decision, cancellationToken);
            if (result is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.OpenDocument, DocumentId: id.ToString()));
            actionBus.Publish(new AppAction(AppActionKind.Highlight, DocumentId: id.ToString(), Target: field.Trim()));
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.FieldCorrected, field.Trim()));
        });

        Task<object> SetReviewStateAsync(string documentId, string decision) => CatchAsync(async () =>
        {
            if (!TryParseId(documentId, out var id))
            {
                return Failure(AgentToolMessages.InvalidDocumentId);
            }

            if (string.IsNullOrWhiteSpace(decision) || !AgentReviewDecisions.Map.TryGetValue(decision.Trim(), out var mapped))
            {
                return Failure(AgentToolMessages.UnknownDecision);
            }

            var result = await workflow.ReviewAsync(
                id,
                new ReviewDecision(mapped, AgentReviewDecisions.Reviewer, null, []),
                cancellationToken);
            if (result is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            actionBus.Publish(new AppAction(AppActionKind.Toast, Message: string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.ReviewStateUpdated, mapped)));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.ReviewStateUpdated, mapped), new { reviewState = mapped });
        });

        Task<object> ExportDocumentAsync(string documentId, string format) => CatchAsync(async () =>
        {
            if (!TryParseId(documentId, out var id))
            {
                return Failure(AgentToolMessages.InvalidDocumentId);
            }

            var export = await workflow.ExportAsync(id, cancellationToken);
            if (export is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            var requestedFormat = string.IsNullOrWhiteSpace(format) ? string.Empty : format.Trim();
            actionBus.Publish(new AppAction(AppActionKind.Toast, Message: string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.Exported, id, requestedFormat)));
            actionBus.Publish(new AppAction(AppActionKind.Navigate, Route: AgentRoutes.Export));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.Exported, id, requestedFormat), new { fieldCount = export.Fields.Count });
        });

        Task<object> FilterQueueAsync(string filter) => CatchAsync(() =>
        {
            var normalized = filter?.Trim() ?? string.Empty;
            actionBus.Publish(new AppAction(AppActionKind.SetFilter, Filter: normalized));
            return Task.FromResult(Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.FilterApplied, normalized)));
        });

        Task<object> ReseedAsync() => CatchAsync(async () =>
        {
            if (maintenance is null)
            {
                return Failure(AgentToolMessages.MaintenanceUnavailable);
            }

            var seeded = await maintenance.ReseedDemoAsync(cancellationToken);
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            var message = seeded ? AgentToolMessages.Reseeded : AgentToolMessages.AlreadySeeded;
            actionBus.Publish(new AppAction(AppActionKind.Toast, Message: message));
            return Success(message, new { seeded });
        });

        Task<object> ClearAsync(bool confirm) => CatchAsync(async () =>
        {
            if (!confirm)
            {
                return Failure(AgentToolMessages.ConfirmationRequired);
            }

            if (maintenance is null)
            {
                return Failure(AgentToolMessages.MaintenanceUnavailable);
            }

            var removed = await maintenance.ClearAllDocumentsAsync(cancellationToken);
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            var message = string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.Cleared, removed);
            actionBus.Publish(new AppAction(AppActionKind.Toast, Message: message));
            return Success(message, new { removed });
        });

        return
        [
            AIFunctionFactory.Create((Func<Task<object>>)ListDocumentsAsync, "list_documents", "List ingested documents with status, classified type, overall confidence, and exception count.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)GetDocumentAsync, "get_document", "Get extracted fields and exceptions for one document by its id.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)ReconcileAsync, "reconcile", "Run reconciliation for a document: compares each stated control total (Detected) against the value computed from line items (Expected).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)ExplainFieldAsync, "explain_field", "Explain where a single extracted field's value came from, with its source citations (page and quote).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)GotoAsync, AgentToolNames.Goto, "Navigate the desktop app to a page. route is one of: dashboard, review, mappings, export, settings.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)OpenDocumentAsync, AgentToolNames.OpenDocument, "Open a document by id in the review view so the user sees it.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<Task<object>>)ProcessDocumentsAsync, AgentToolNames.ProcessDocuments, "Refresh the document queue and report how many documents are present.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, string, Task<object>>)CorrectFieldAsync, AgentToolNames.CorrectField, "Correct a single extracted field on a document (sets it to NeedsCorrection) and highlight the field in the review view.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)SetReviewStateAsync, AgentToolNames.SetReviewState, "Set a document's review state. decision is one of: approve, reject, needscorrection.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)ExportDocumentAsync, AgentToolNames.ExportDocument, "Export a document and open the export view. format is the requested output format (for example csv or xlsx).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)FilterQueueAsync, AgentToolNames.FilterQueue, "Set the document queue filter in the dashboard.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<Task<object>>)ReseedAsync, AgentToolNames.Reseed, "Reseed the demo document corpus when the workspace is empty.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<bool, Task<object>>)ClearAsync, AgentToolNames.Clear, "Delete every document. Destructive: only proceeds when confirm is true.", ToolJsonOptions)
        ];
    }

    private static async Task<object> CatchAsync(Func<Task<object>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new { error = ex.Message };
        }
    }

    private static Task<DocumentRecord?> LoadDocumentAsync(RevaDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.AsSplitQuery()
            .Include(item => item.Fields)
            .Include(item => item.Tables)
            .Include(item => item.Exceptions)
            .Include(item => item.SourceSpans)
            .Include(item => item.Pages)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static bool TryParseId(string? documentId, out Guid id)
    {
        if (!string.IsNullOrWhiteSpace(documentId) && Guid.TryParse(documentId.Trim(), out id))
        {
            return true;
        }

        id = Guid.Empty;
        return false;
    }

    private static object Success(string message) => new { success = true, message };

    private static object Success(string message, object data) => new { success = true, message, data };

    private static object Failure(string message) => new { success = false, message };
}
