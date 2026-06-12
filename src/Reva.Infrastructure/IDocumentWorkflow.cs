using Reva.Core.Contracts;

namespace Reva.Infrastructure;

public interface IDocumentWorkflow
{
    Task<DocumentUploadResult> IngestAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken cancellationToken);
    Task<DocumentDetail?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DocumentDetail?> ReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken);
    Task<ExportRecord?> ExportAsync(Guid id, CancellationToken cancellationToken);
}

