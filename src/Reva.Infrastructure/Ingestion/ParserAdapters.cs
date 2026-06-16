using Microsoft.Extensions.Options;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Ingestion;

public sealed class DefaultDocumentParserAdapter(IDocumentParser parser) : IDocumentParserAdapter
{
    public string Name => "default";
    public bool CanParse(string filePath) => true;
    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken) => parser.ParseAsync(filePath, cancellationToken);
}

public sealed class OptionalDoclingDocumentParser(IOptions<DoclingFeatureOptions> options, IDocumentParser parser) : IDocumentParserAdapter
{
    public string Name => "docling";
    public bool CanParse(string filePath) => options.Value.Enabled;
    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken) => parser.ParseAsync(filePath, cancellationToken);
}

public sealed class DoclingFeatureOptions
{
    public bool Enabled { get; set; }
}
