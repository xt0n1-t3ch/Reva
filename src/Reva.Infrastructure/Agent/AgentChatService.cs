using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;

namespace Reva.Infrastructure.Agent;

public interface IAgentChatService
{
    IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, CancellationToken cancellationToken);
    IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken);
}

public sealed class AgentChatService(IChatClient chatClient, IOptions<AgentChatOptions> options) : IAgentChatService
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

    public IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken)
    {
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

        return
        [
            AIFunctionFactory.Create((Func<Task<object>>)ListDocumentsAsync, "list_documents", "List ingested documents with status, classified type, overall confidence, and exception count.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)GetDocumentAsync, "get_document", "Get extracted fields and exceptions for one document by its id.", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, Task<object>>)ReconcileAsync, "reconcile", "Run reconciliation for a document: compares each stated control total (Detected) against the value computed from line items (Expected).", ToolJsonOptions),
            AIFunctionFactory.Create((Func<string, string, Task<object>>)ExplainFieldAsync, "explain_field", "Explain where a single extracted field's value came from, with its source citations (page and quote).", ToolJsonOptions)
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
}
