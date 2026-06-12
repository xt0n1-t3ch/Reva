using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Extraction;

public sealed class ReinsuranceClassifier : IReinsuranceClassifier
{
    private static readonly IReadOnlyDictionary<ReinsuranceDocumentType, string[]> Signals = new Dictionary<ReinsuranceDocumentType, string[]>
    {
        [ReinsuranceDocumentType.Bordereau] = ["bordereau", "policy number", "insured", "gross premium", "cession"],
        [ReinsuranceDocumentType.StatementOfAccount] = ["statement of account", "technical account", "balance due", "commission", "premium"],
        [ReinsuranceDocumentType.LossRun] = ["loss run", "claim number", "paid loss", "outstanding", "incurred"],
        [ReinsuranceDocumentType.Treaty] = ["treaty", "quota share", "excess of loss", "retention", "limit"],
        [ReinsuranceDocumentType.FacultativeSlip] = ["facultative", "risk slip", "sum insured", "placement"],
        [ReinsuranceDocumentType.Endorsement] = ["endorsement", "amendment", "effective date"],
        [ReinsuranceDocumentType.ClaimNotice] = ["claim notice", "date of loss", "reserve"]
    };

    public ClassificationResult Classify(ParsedDocument parsedDocument)
    {
        var text = parsedDocument.Text.ToLowerInvariant();
        var bestType = ReinsuranceDocumentType.Unknown;
        var bestScore = 0;

        foreach (var signalSet in Signals)
        {
            var score = signalSet.Value.Count(text.Contains);
            if (score > bestScore)
            {
                bestType = signalSet.Key;
                bestScore = score;
            }
        }

        if (bestType == ReinsuranceDocumentType.Unknown)
        {
            return new ClassificationResult(bestType, 0.35);
        }

        var confidence = Math.Min(0.96, 0.45 + (bestScore * 0.12));
        return new ClassificationResult(bestType, confidence);
    }
}

