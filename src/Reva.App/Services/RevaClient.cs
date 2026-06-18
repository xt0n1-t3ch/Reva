using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reva.Ai;
using Reva.Ai.Models;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Settings;
using Reva.Infrastructure;
using Reva.Infrastructure.Export;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.App.Services;

public sealed class RevaClient(IServiceScopeFactory scopeFactory, IModelRegistry modelRegistry) : IRevaClient
{
    private const string FallbackContentType = "application/octet-stream";

    public async Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        return await workflow.ListAsync(cancellationToken);
    }

    public async Task<DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        return await workflow.GetAsync(id, cancellationToken);
    }

    public async Task<BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
        var assembler = scope.ServiceProvider.GetRequiredService<IBdxReviewPayloadAssembler>();
        var record = await LoadDocumentAsync(dbContext, id, cancellationToken);
        return record is null ? null : assembler.Assemble(record);
    }

    public async Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The selected file does not exist.", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        return await UploadAsync(Path.GetFileName(filePath), ResolveContentType(filePath), stream, cancellationToken);
    }

    public async Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        return await workflow.IngestAsync(fileName, contentType, content, cancellationToken);
    }

    public async Task<DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        return await workflow.ReviewAsync(id, decision, cancellationToken);
    }

    public async Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.ListAsync(cancellationToken);
    }

    public async Task<ExportTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.GetAsync(id, cancellationToken);
    }

    public async Task<ExportTemplate> CreateTemplateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.CreateAsync(draft, cancellationToken);
    }

    public async Task<ExportTemplate?> UpdateTemplateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.UpdateAsync(id, draft, cancellationToken);
    }

    public async Task<ExportTemplate?> DuplicateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.DuplicateAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        return await store.DeleteAsync(id, cancellationToken);
    }

    public async Task<ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        var exporter = scope.ServiceProvider.GetRequiredService<IDocumentExporter>();
        var document = await workflow.GetAsync(documentId, cancellationToken);
        var template = await store.GetAsync(templateId, cancellationToken);
        return document is null || template is null ? null : exporter.Preview(document, template);
    }

    public async Task<ExportFile?> ExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        var store = scope.ServiceProvider.GetRequiredService<IExportTemplateStore>();
        var exporter = scope.ServiceProvider.GetRequiredService<IDocumentExporter>();
        var document = await workflow.GetAsync(documentId, cancellationToken);
        var template = await store.GetAsync(templateId, cancellationToken);
        return document is null || template is null ? null : exporter.Export(document, template);
    }

    public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
        return await store.GetAsync(cancellationToken);
    }

    public async Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<ISettingsStore>();
        return await store.SaveAsync(settings, cancellationToken);
    }

    public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default) =>
        modelRegistry.ListAsync(cancellationToken);

    public Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default) =>
        modelRegistry.GetActiveModelAsync(cancellationToken);

    public Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default) =>
        modelRegistry.SetActiveModelAsync(modelId, cancellationToken);

    public Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default) =>
        modelRegistry.IsOllamaAvailableAsync(cancellationToken);

    private static Task<DocumentRecord?> LoadDocumentAsync(RevaDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents
            .AsSplitQuery()
            .Include(document => document.Fields)
            .Include(document => document.Tables)
            .Include(document => document.SchemaMappings)
            .Include(document => document.SourceSpans)
            .Include(document => document.Pages)
            .Include(document => document.Exceptions)
            .Include(document => document.ReviewEvents)
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

    private static string ResolveContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return DocumentContentTypes.Map.TryGetValue(extension, out var contentType) ? contentType : FallbackContentType;
    }
}
