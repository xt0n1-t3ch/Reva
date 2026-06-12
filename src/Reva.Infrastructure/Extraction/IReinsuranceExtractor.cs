using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Extraction;

public sealed record ReinsuranceExtractionResult(
    ReinsuranceDocumentType DocumentType,
    double Confidence,
    IReadOnlyList<ExtractedField> Fields,
    IReadOnlyList<ExtractedTable> Tables,
    IReadOnlyList<ExtractionIssue> Exceptions);

public interface IReinsuranceExtractor
{
    ReinsuranceExtractionResult Extract(ParsedDocument parsedDocument, ClassificationResult classificationResult);
}

