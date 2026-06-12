using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Extraction;

public sealed record ClassificationResult(ReinsuranceDocumentType DocumentType, double Confidence);

public interface IReinsuranceClassifier
{
    ClassificationResult Classify(ParsedDocument parsedDocument);
}

