using Reva.Core.Contracts;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.SchemaMapping;

public sealed record SchemaMappingResult(
    IReadOnlyList<ExtractedField> Fields,
    IReadOnlyList<DocumentSchemaMappingRecord> Mappings);

public interface ISchemaMappingService
{
    Task<SchemaMappingResult> MapAsync(ParsedDocument parsedDocument, IReadOnlyList<ExtractedField> fields, CancellationToken cancellationToken);
    Task LearnAsync(string senderKey, IReadOnlyList<SchemaMappingCorrection> corrections, CancellationToken cancellationToken);
}
