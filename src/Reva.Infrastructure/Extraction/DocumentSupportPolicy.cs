using Reva.Core.Contracts;
using Reva.Core.Reinsurance;

namespace Reva.Infrastructure.Extraction;

public static class DocumentSupportPolicy
{
    public const double MinimumClassificationConfidence = 0.45;

    public const string UnsupportedDocumentMessage = "Unsupported document: upload a reinsurance technical account, bordereau, statement of account, treaty, loss run, endorsement, facultative slip, or claim notice.";

    public static bool IsSupported(ClassificationResult classification)
    {
        return classification.DocumentType != ReinsuranceDocumentType.Unknown
            && classification.Confidence >= MinimumClassificationConfidence;
    }

    public static ExtractionIssue UnsupportedIssue()
    {
        return new ExtractionIssue(ExceptionSeverity.Warning, UnsupportedDocumentMessage);
    }
}
