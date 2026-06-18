using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Reva.Ai.Models;
using Reva.Core.Contracts;
using Reva.Core.Settings;

namespace Reva.App.Services;

public interface IRevaClient
{
    Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    Task<DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default);

    Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);

    Task<DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task<ExportTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ExportTemplate> CreateTemplateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken = default);

    Task<ExportTemplate?> UpdateTemplateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken = default);

    Task<ExportTemplate?> DuplicateTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default);

    Task<ExportFile?> ExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default);

    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default);

    Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default);

    Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default);

    Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default);
}
