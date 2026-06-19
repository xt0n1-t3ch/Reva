using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Core.Settings;
using Reva.Infrastructure.Export;
using Reva.Infrastructure.Knowledge;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.Infrastructure.Agent;

public interface IAgentChatService
{
    IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, CancellationToken cancellationToken, AgentReasoningOptions? reasoning = null);
    IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken, IDataMaintenance? maintenance = null);
}

public sealed class AgentChatService(ILlmChatClientFactory chatClientFactory, IOptions<AgentChatOptions> options, IAppActionBus actionBus, IDocumentExporter exporter, IKnowledgeStore? knowledgeStore = null) : IAgentChatService
{
    private const string SqliteNoCaseCollation = "NOCASE";
    private const string LikeEscape = "\\";

    private static readonly JsonSerializerOptions ToolJsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };

    public IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, CancellationToken cancellationToken, AgentReasoningOptions? reasoning = null)
    {
        var settings = RuntimeSettings.Current;
        var provider = AiProviderNames.Normalize(settings.AiProvider);
        var baseUrl = LlmChatClientFactory.NormalizeOpenAiBaseUrl(provider, string.IsNullOrWhiteSpace(settings.AiBaseUrl) ? options.Value.BaseUrl : settings.AiBaseUrl);
        var model = AiSettingsDefaults.NormalizeModel(string.IsNullOrWhiteSpace(settings.AiModel) ? options.Value.Model : settings.AiModel);
        var client = chatClientFactory.Create(new LlmChatClientRequest(
            provider,
            baseUrl,
            settings.AiApiKey,
            model,
            options.Value.MaxSteps,
            options.Value.NumCtx));

        return client.GetStreamingResponseAsync(
            messages,
            new ChatOptions
            {
                Tools = [.. tools],
                Temperature = (float)options.Value.Temperature,
                ModelId = model,
                AdditionalProperties = AgentReasoningMapper.BuildAdditionalProperties(options.Value.NumCtx, provider, model, reasoning)
            },
            cancellationToken);
    }

    public IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken, IDataMaintenance? maintenance = null)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(assembler);

        var store = knowledgeStore ?? new EmbeddedKnowledgeStore();

        Task<object> ListDocumentsAsync() => CatchAsync(async () =>
        {
            var documents = await workflow.ListAsync(cancellationToken);
            return new
            {
                count = documents.Count,
                documents = documents.Select((document, index) => new
                {
                    fileName = document.FileName,
                    index = index + 1,
                    status = document.Status,
                    type = document.DocumentType,
                    confidence = Round(document.Confidence),
                    exceptions = document.ExceptionCount,
                    reviewState = document.ReviewState
                }).ToList()
            };
        });

        Task<object> GetDocumentAsync([Description("The document's file name (preferred) or its id.")] string documentId) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            var detail = await workflow.GetAsync(id.Value, cancellationToken);
            if (detail is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            return new
            {
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

        Task<object> ReconcileAsync([Description("The document's file name (preferred) or its id.")] string documentId) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            var record = await LoadDocumentAsync(dbContext, id.Value, cancellationToken);
            if (record is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            var checks = assembler.Assemble(record).Reconciliation;
            return new
            {
                fileName = record.FileName,
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

        Task<object> ExplainFieldAsync([Description("The document's file name (preferred) or its id.")] string documentId, string field) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            var record = await LoadDocumentAsync(dbContext, id.Value, cancellationToken);
            if (record is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
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
                fileName = record.FileName,
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

        Task<object> OpenDocumentAsync([Description("The document's file name (preferred) or its id.")] string documentId) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            var detail = await workflow.GetAsync(id.Value, cancellationToken);
            if (detail is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.OpenDocument, DocumentId: id.Value.ToString()));
            actionBus.Publish(new AppAction(AppActionKind.Navigate, Route: AgentRoutes.Review));
            return Success($"Opened document {detail.FileName}.");
        });

        Task<object> RefreshQueueAsync() => CatchAsync(async () =>
        {
            var documents = await workflow.ListAsync(cancellationToken);
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.QueueState, documents.Count), new { count = documents.Count });
        });

        Task<object> CorrectFieldAsync([Description("The document's file name (preferred) or its id.")] string documentId, string field, string value) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
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
            var result = await workflow.ReviewAsync(id.Value, decision, cancellationToken);
            if (result is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.OpenDocument, DocumentId: id.Value.ToString()));
            actionBus.Publish(new AppAction(AppActionKind.Highlight, DocumentId: id.Value.ToString(), Target: field.Trim()));
            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.FieldCorrected, field.Trim(), result.FileName), new { fileName = result.FileName });
        });

        Task<object> SetReviewStateAsync([Description("The document's file name (preferred) or its id.")] string documentId, string decision) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            if (string.IsNullOrWhiteSpace(decision) || !AgentReviewDecisions.Map.TryGetValue(decision.Trim(), out var mapped))
            {
                return Failure(AgentToolMessages.UnknownDecision);
            }

            var result = await workflow.ReviewAsync(
                id.Value,
                new ReviewDecision(mapped, AgentReviewDecisions.Reviewer, null, []),
                cancellationToken);
            if (result is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            actionBus.Publish(new AppAction(AppActionKind.Refresh));
            return Success(string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.ReviewStateUpdated, result.FileName, mapped), new { fileName = result.FileName, reviewState = mapped });
        });

        Task<object> ExportDocumentAsync([Description("The document's file name (preferred) or its id.")] string documentId, string format) => CatchAsync(async () =>
        {
            var id = await ResolveDocumentIdAsync(documentId, dbContext, cancellationToken);
            if (id is null)
            {
                return Failure(AgentToolMessages.DocumentReferenceNotFound);
            }

            var detail = await workflow.GetAsync(id.Value, cancellationToken);
            if (detail is null)
            {
                return Failure(AgentToolMessages.DocumentNotFound);
            }

            if (!TryResolveExportFormat(format, out var exportFormat, out var requestedFormat))
            {
                return Failure(AgentToolMessages.UnsupportedExportFormat);
            }

            var file = exporter.Export(detail, ResolveExportTemplate(exportFormat));
            var path = await WriteExportAsync(file, cancellationToken);
            actionBus.Publish(new AppAction(AppActionKind.Navigate, Route: AgentRoutes.Export));
            return Success(
                string.Format(CultureInfo.InvariantCulture, AgentToolMessageFormats.Exported, detail.FileName, requestedFormat, path),
                new { documentFileName = detail.FileName, path, fileName = file.FileName, contentType = file.ContentType, fieldCount = detail.Fields.Count });
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
            return Success(message, new { removed });
        });

        Task<object> SearchKnowledgeAsync([Description("Product, methodology, or reinsurance-industry question to search for in Reva's built-in knowledge base.")] string query) => CatchAsync(async () =>
        {
            var matches = await store.SearchAsync(query ?? string.Empty, 4, cancellationToken);
            return matches.Select(match => new
            {
                slug = match.Slug,
                title = match.Title,
                snippet = match.Snippet
            }).ToList();
        });

        Task<object> CurrentDateTimeAsync() => CatchAsync(() =>
        {
            var now = DateTimeOffset.Now;
            return Task.FromResult<object>(new
            {
                iso = now.ToString("O", CultureInfo.InvariantCulture),
                date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                time = now.ToString("HH:mm", CultureInfo.InvariantCulture),
                dayOfWeek = now.DayOfWeek.ToString(),
                timeZone = TimeZoneInfo.Local.StandardName,
                utcOffset = now.ToString("zzz", CultureInfo.InvariantCulture)
            });
        });

        return
        [
            AIFunctionFactory.Create((Func<Task<object>>)CurrentDateTimeAsync, "get_current_datetime", "Get the current date, day of the week, local time, and time zone. Use this for any question about today, now, or the current date or time.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<Task<object>>)ListDocumentsAsync, "list_documents", "List ingested documents with status, classified type, overall confidence, and exception count.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)GetDocumentAsync, "get_document", "Get extracted fields and exceptions for one document by file name.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)ReconcileAsync, "reconcile", "Run reconciliation for a document by file name: compares each stated control total (Detected) against the value computed from line items (Expected).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)ExplainFieldAsync, "explain_field", "Explain where a single extracted field's value came from for a document by file name, with its source citations (page and quote).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)GotoAsync, AgentToolNames.Goto, "Navigate the desktop app to a page. route is one of: dashboard, review, mappings, export, settings.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)OpenDocumentAsync, AgentToolNames.OpenDocument, "Open a document by file name in the review view so the user sees it.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<Task<object>>)RefreshQueueAsync, AgentToolNames.RefreshQueue, "Refresh the document queue and report how many documents are present.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, string, Task<object>>)CorrectFieldAsync, AgentToolNames.CorrectField, "Correct a single extracted field on a document by file name (sets it to NeedsCorrection) and highlight the field in the review view.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)SetReviewStateAsync, AgentToolNames.SetReviewState, "Set a document's review state by file name. decision is one of: approve, reject, needscorrection.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)ExportDocumentAsync, AgentToolNames.ExportDocument, "Export a document by file name to a real file. format is csv, xlsx, or json.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)FilterQueueAsync, AgentToolNames.FilterQueue, "Request a document queue filter in the dashboard.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<Task<object>>)ReseedAsync, AgentToolNames.Reseed, "Reseed the demo document corpus when the workspace is empty.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<bool, Task<object>>)ClearAsync, AgentToolNames.Clear, "Delete every document. Destructive: only proceeds when confirm is true.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)SearchKnowledgeAsync, AgentToolNames.SearchKnowledge, "Search Reva's built-in knowledge base for product, methodology, and reinsurance industry answers. query is a short keyword phrase.", ToolJsonOptions)
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
            .AsNoTracking()
            .Include(item => item.Fields)
            .Include(item => item.Tables)
            .Include(item => item.Exceptions)
            .Include(item => item.SourceSpans)
            .Include(item => item.Pages)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    private static async Task<Guid?> ResolveDocumentIdAsync(string? reference, RevaDbContext dbContext, CancellationToken cancellationToken)
    {
        var normalizedReference = reference?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            return null;
        }

        if (Guid.TryParse(normalizedReference, out var parsedId))
        {
            var existingId = await dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == parsedId)
                .Select(document => (Guid?)document.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId is not null)
            {
                return existingId;
            }
        }

        var exactId = await dbContext.Documents
            .AsNoTracking()
            .Where(document => EF.Functions.Collate(document.FileName, SqliteNoCaseCollation) == normalizedReference)
            .Select(document => (Guid?)document.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (exactId is not null)
        {
            return exactId;
        }

        var escapedReference = EscapeLikePattern(normalizedReference);
        var startsWithPattern = $"{escapedReference}%";
        var endsWithPattern = $"%{escapedReference}";
        var matchedIds = await dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                EF.Functions.Like(EF.Functions.Collate(document.FileName, SqliteNoCaseCollation), startsWithPattern, LikeEscape)
                || EF.Functions.Like(EF.Functions.Collate(document.FileName, SqliteNoCaseCollation), endsWithPattern, LikeEscape))
            .Select(document => document.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchedIds.Count == 1 ? matchedIds[0] : null;
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    private static bool TryResolveExportFormat(string? format, out ExportFormat exportFormat, out string requestedFormat)
    {
        requestedFormat = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        switch (requestedFormat)
        {
            case "csv":
                exportFormat = ExportFormat.Csv;
                requestedFormat = "csv";
                return true;
            case "xlsx":
            case "excel":
                exportFormat = ExportFormat.Excel;
                requestedFormat = "xlsx";
                return true;
            case "json":
                exportFormat = ExportFormat.Json;
                return true;
            default:
                exportFormat = ExportFormat.Csv;
                return false;
        }
    }

    private static ExportTemplate ResolveExportTemplate(ExportFormat exportFormat) =>
        ExportTemplateDefaults.All.First(template => template.Format == exportFormat);

    private static async Task<string> WriteExportAsync(ExportFile file, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), "reva-agent-exports");
        Directory.CreateDirectory(directory);
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(path, file.Content, cancellationToken);
        return path;
    }

    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static object Success(string message) => new { success = true, message };

    private static object Success(string message, object data) => new { success = true, message, data };

    private static object Failure(string message) => new { success = false, message };
}
