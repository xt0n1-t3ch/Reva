namespace Reva.Infrastructure.Storage;

public interface IDocumentStorage
{
    Task<string> SaveAsync(Guid documentId, string fileName, Stream content, CancellationToken cancellationToken);
}

