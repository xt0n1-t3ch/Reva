namespace Reva.Infrastructure.Hashing;

public interface IDocumentHasher
{
    Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken);
}

