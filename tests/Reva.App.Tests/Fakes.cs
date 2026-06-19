using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Reva.Ai;
using Reva.Ai.Models;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Settings;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.App.Tests;

internal sealed class FakeRevaClient : IRevaClient
{
    public IReadOnlyList<DocumentSummary> Documents { get; init; } = [];

    public IReadOnlyList<ModelDescriptor> Models { get; init; } = [];

    public IReadOnlyList<ExportTemplate> Templates { get; init; } = [];

    public BdxReviewPayload? ReviewPayload { get; init; }

    public Guid? ReviewDocumentId { get; init; }

    public string? ActiveModel { get; private set; }

    public bool OllamaOnline { get; init; }

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public AppSettings? LastSavedSettings { get; private set; }

    public string? LastSetModelId { get; private set; }

    public Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Documents);

    public Task<DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<DocumentDetail?>(null);

    public Task<BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (ReviewPayload is not null && (ReviewDocumentId is null || ReviewDocumentId == id))
        {
            return Task.FromResult<BdxReviewPayload?>(ReviewPayload);
        }

        return Task.FromResult<BdxReviewPayload?>(null);
    }

    public Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
        => Task.FromResult<DocumentDetail?>(null);

    public Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Templates);

    public Task<ExportTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<ExportTemplate?>(null);

    public Task<ExportTemplate> CreateTemplateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<ExportTemplate?> UpdateTemplateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult<ExportTemplate?>(null);

    public Task<ExportTemplate?> DuplicateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<ExportTemplate?>(null);

    public Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
        => Task.FromResult<ExportPreview?>(null);

    public Task<ExportFile?> ExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
        => Task.FromResult<ExportFile?>(null);

    public Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Settings);

    public Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        LastSavedSettings = settings;
        Settings = settings;
        return Task.FromResult(settings);
    }

    public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Models);

    public Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(ActiveModel);

    public Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        LastSetModelId = modelId;
        ActiveModel = modelId;
        return Task.CompletedTask;
    }

    public Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(OllamaOnline);
}

internal sealed class FakeModelRegistry(bool online, string? activeModel = null) : IModelRegistry
{
    public Task<IReadOnlyList<ModelDescriptor>> ListAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ModelDescriptor>>([]);

    public Task<string?> GetActiveModelAsync(CancellationToken ct)
        => Task.FromResult(activeModel);

    public Task SetActiveModelAsync(string modelId, CancellationToken ct)
        => Task.CompletedTask;

    public Task<bool> IsOllamaAvailableAsync(CancellationToken ct)
        => Task.FromResult(online);
}

internal sealed class FakeAgentChatService : IAgentChatService
{
    public IReadOnlyList<AITool> BuildTools(
        IDocumentWorkflow workflow,
        RevaDbContext dbContext,
        IBdxReviewPayloadAssembler assembler,
        CancellationToken cancellationToken,
        IDataMaintenance? maintenance = null)
        => [];

#pragma warning disable CS1998
    public async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<AITool> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield break;
    }
#pragma warning restore CS1998
}
