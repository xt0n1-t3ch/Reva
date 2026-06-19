using Microsoft.Extensions.Options;

namespace Reva.Infrastructure.Ingestion;

public sealed class FileEmailInboundOptions
{
    public string Directory { get; set; } = string.Empty;
}

public sealed class FileEmailInboundDocumentSource(IOptions<FileEmailInboundOptions> options) : IInboundDocumentSource
{
    public string Name => "file-email";

    public Task<IReadOnlyList<InboundDocument>> PullAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Directory) || !System.IO.Directory.Exists(options.Value.Directory))
        {
            return Task.FromResult<IReadOnlyList<InboundDocument>>([]);
        }

        IReadOnlyList<InboundDocument> documents = System.IO.Directory.EnumerateFiles(options.Value.Directory)
            .Where(path => Path.GetExtension(path).Equals(".eml", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".msg", StringComparison.OrdinalIgnoreCase))
            .Select(path => new InboundDocument(Path.GetFileName(path), "message/rfc822", File.OpenRead(path)))
            .ToList();
        return Task.FromResult(documents);
    }
}

public sealed class DisabledOAuthInboundDocumentSource(string name) : IInboundDocumentSource
{
    public string Name { get; } = name;
    public Task<IReadOnlyList<InboundDocument>> PullAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<InboundDocument>>([]);
}

public sealed class InboundSourceRegistry(IEnumerable<IInboundDocumentSource> sources) : IInboundSourceRegistry
{
    public IReadOnlyList<InboundSourceStatus> Statuses() =>
    [
        .. sources.Select(source => source.Name switch
        {
            "gmail" => new InboundSourceStatus(source.Name, false, "disabled: requires OAuth credentials + network"),
            "outlook" => new InboundSourceStatus(source.Name, false, "disabled: requires OAuth credentials + network"),
            _ => new InboundSourceStatus(source.Name, true, "enabled")
        }),
        new InboundSourceStatus("docling", false, "disabled: parser adapter not wired")
    ];
}
