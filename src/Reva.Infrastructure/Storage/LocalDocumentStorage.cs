using Microsoft.Extensions.Options;

namespace Reva.Infrastructure.Storage;

public sealed class LocalDocumentStorage(IOptions<RevaStorageOptions> options) : IDocumentStorage
{
    public async Task<string> SaveAsync(Guid documentId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var root = Path.GetFullPath(options.Value.UploadRoot);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"{documentId:N}-{safeFileName}");
        await using var target = File.Create(path);
        await content.CopyToAsync(target, cancellationToken);
        return path;
    }
}

