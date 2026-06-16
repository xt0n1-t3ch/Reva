using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Ingestion;

public sealed record InboundDocument(string FileName, string ContentType, Stream Content);

public sealed record InboundSourceStatus(string Name, bool Enabled, string Status);

public interface IInboundDocumentSource
{
    string Name { get; }
    Task<IReadOnlyList<InboundDocument>> PullAsync(CancellationToken cancellationToken);
}

public interface IDocumentParserAdapter
{
    string Name { get; }
    bool CanParse(string filePath);
    Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken);
}

public interface IInboundSourceRegistry
{
    IReadOnlyList<InboundSourceStatus> Statuses();
}
