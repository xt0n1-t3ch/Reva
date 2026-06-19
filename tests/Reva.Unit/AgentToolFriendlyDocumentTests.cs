using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Export;
using Reva.Core.Reinsurance;
using Reva.Core.Settings;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Export;
using Reva.Infrastructure.Knowledge;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;

namespace Reva.Unit;

public sealed class AgentToolFriendlyDocumentTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task ListDocumentsResultUsesFileNameWithoutModelFacingId()
    {
        var id = Guid.NewGuid();
        const string fileName = "orion-property-cat-xl-jan-2025.eml";
        await using var context = await CreateContextAsync(DocumentRecord(id, fileName));
        var workflow = new StubWorkflow([DocumentDetail(id, fileName)]);
        var tools = BuildTools(workflow, context);
        var tool = GetFunction(tools, "list_documents");

        using var result = await InvokeAsync(tool, new Dictionary<string, object?>());
        var root = result.RootElement;
        var document = root.GetProperty("documents").EnumerateArray().Single();

        Assert.Equal(1, document.GetProperty("index").GetInt32());
        Assert.Equal(fileName, document.GetProperty("fileName").GetString());
        Assert.False(document.TryGetProperty("id", out _));
        Assert.DoesNotContain(id.ToString(), root.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetDocumentResolvesDocumentIdByFileNameOrGuid(bool useFileName)
    {
        var id = Guid.NewGuid();
        const string fileName = "orion-property-cat-xl-jan-2025.eml";
        await using var context = await CreateContextAsync(DocumentRecord(id, fileName));
        var workflow = new StubWorkflow([DocumentDetail(id, fileName)]);
        var tools = BuildTools(workflow, context);
        var tool = GetFunction(tools, "get_document");

        var reference = useFileName ? fileName : id.ToString();
        using var result = await InvokeAsync(tool, new Dictionary<string, object?> { ["documentId"] = reference });
        var root = result.RootElement;

        Assert.Equal(id, workflow.LastGetId);
        Assert.Equal(fileName, root.GetProperty("fileName").GetString());
        Assert.False(root.TryGetProperty("id", out _));
        Assert.DoesNotContain(id.ToString(), root.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("orion")]
    [InlineData("missing.eml")]
    public async Task GetDocumentReturnsCleanDisambiguationForAmbiguousOrUnknownName(string reference)
    {
        await using var context = await CreateContextAsync(
            DocumentRecord(Guid.NewGuid(), "orion-property-cat-xl-jan-2025.eml"),
            DocumentRecord(Guid.NewGuid(), "orion-property-cat-xl-feb-2025.eml"));
        var workflow = new StubWorkflow([]);
        var tools = BuildTools(workflow, context);
        var tool = GetFunction(tools, "get_document");

        using var result = await InvokeAsync(tool, new Dictionary<string, object?> { ["documentId"] = reference });
        var root = result.RootElement;
        var message = Assert.IsType<string>(root.GetProperty("message").GetString());

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("full file name", message, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext context)
    {
        var service = new AgentChatService(
            new StubChatClientFactory(),
            Options.Create(new AgentChatOptions()),
            new AppActionBus(),
            new StubExporter());

        return service.BuildTools(workflow, context, new StubAssembler(), CancellationToken.None);
    }

    private static AIFunction GetFunction(IReadOnlyList<AITool> tools, string name) =>
        Assert.IsAssignableFrom<AIFunction>(tools.Single(tool => tool.Name == name));

    private static async Task<JsonDocument> InvokeAsync(AIFunction tool, IDictionary<string, object?> arguments)
    {
        var result = await tool.InvokeAsync(new AIFunctionArguments(arguments), CancellationToken.None);
        return JsonDocument.Parse(JsonSerializer.Serialize(result, SerializerOptions));
    }

    private static async Task<RevaDbContext> CreateContextAsync(params DocumentRecord[] documents)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var context = new RevaDbContext(new DbContextOptionsBuilder<RevaDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();
        return context;
    }

    private static DocumentRecord DocumentRecord(Guid id, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DocumentRecord
        {
            Id = id,
            FileName = fileName,
            Sha256Hash = Convert.ToHexString(Guid.NewGuid().ToByteArray()),
            Extension = Path.GetExtension(fileName),
            StoragePath = fileName,
            Status = DocumentStatus.Extracted.ToString(),
            ReviewState = ReviewState.Pending.ToString(),
            DocumentType = ReinsuranceDocumentType.Bordereau.ToString(),
            Confidence = 0.91d,
            ParsedMarkdown = string.Empty,
            ParsedJson = "{}",
            ParserProfile = "test",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static DocumentDetail DocumentDetail(Guid id, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DocumentDetail(
            id,
            fileName,
            "sha",
            DocumentStatus.Extracted,
            ReviewState.Pending,
            ReinsuranceDocumentType.Bordereau,
            0.91d,
            string.Empty,
            "test",
            [new ExtractedField("Premium", "USD 100", 0.9d, "regex", false)],
            [],
            [],
            now,
            now);
    }

    private sealed class StubWorkflow(IReadOnlyList<DocumentDetail> documents) : IDocumentWorkflow
    {
        private readonly IReadOnlyDictionary<Guid, DocumentDetail> _documents = documents.ToDictionary(document => document.Id);

        public Guid? LastGetId { get; private set; }

        public Task<DocumentUploadResult> IngestAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DocumentSummary>>(_documents.Values.Select(document => new DocumentSummary(
                document.Id,
                document.FileName,
                document.Status,
                document.ReviewState,
                document.DocumentType,
                document.Confidence,
                document.Exceptions.Count,
                document.CreatedAt,
                document.UpdatedAt)).ToList());

        public Task<DocumentDetail?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            LastGetId = id;
            return Task.FromResult(_documents.GetValueOrDefault(id));
        }

        public Task<DocumentDetail?> ReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken) =>
            Task.FromResult(_documents.GetValueOrDefault(id));

        public Task<ExportRecord?> ExportAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubAssembler : IBdxReviewPayloadAssembler
    {
        public BdxReviewPayload Assemble(DocumentRecord document) =>
            throw new NotSupportedException();
    }

    private sealed class StubExporter : IDocumentExporter
    {
        public ExportPreview Preview(DocumentDetail document, ExportTemplate layout, int maxRows = 6) =>
            throw new NotSupportedException();

        public ExportFile Export(DocumentDetail document, ExportTemplate layout) =>
            throw new NotSupportedException();
    }

    private sealed class StubChatClientFactory : ILlmChatClientFactory
    {
        public IChatClient Create(LlmChatClientRequest request) => new StubChatClient();
    }

    private sealed class StubChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("ok")]);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
