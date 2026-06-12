namespace Reva.Infrastructure.Parsing;

public interface IDocumentParser
{
    Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken);
}

