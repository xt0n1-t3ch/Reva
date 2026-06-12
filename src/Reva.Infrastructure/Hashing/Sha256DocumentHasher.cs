using System.Security.Cryptography;

namespace Reva.Infrastructure.Hashing;

public sealed class Sha256DocumentHasher : IDocumentHasher
{
    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        stream.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

