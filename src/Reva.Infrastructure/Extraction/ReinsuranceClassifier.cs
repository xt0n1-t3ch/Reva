using Reva.Core.Contracts;
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

    // Columns that mark a row-per-risk bordereau table rather than a prose statement.
    private static readonly string[] BordereauColumns =
        ["cedent", "member", "premium", "claims", "commission", "cession", "ceded", "line of business", "line no"];

    public ClassificationResult Classify(ParsedDocument parsedDocument)
    {
        var text = parsedDocument.Text.ToLowerInvariant();
        var scores = Signals.ToDictionary(entry => entry.Key, entry => (double)entry.Value.Count(text.Contains));

        // A data table with several reinsurance financial columns is strong evidence of a
        // bordereau — this is what a row-per-risk register looks like, and it outweighs the
        // generic "premium"/"commission" words that also appear in statements of account.
        if (HasBordereauTable(parsedDocument.Tables))
        {
            scores[ReinsuranceDocumentType.Bordereau] += 4;
        }

        var best = scores.OrderByDescending(entry => entry.Value).First();
        if (best.Value <= 0)
        {
            return new ClassificationResult(ReinsuranceDocumentType.Unknown, 0.35);
        }

        var confidence = Math.Min(0.97, 0.5 + (best.Value * 0.1));
        return new ClassificationResult(best.Key, Math.Round(confidence, 2));
    }

    private static bool HasBordereauTable(IReadOnlyList<ExtractedTable> tables) =>
        tables.Any(table => table.Rows.Count > 0
            && table.Headers.Count(header => BordereauColumns.Any(column =>
                header.Contains(column, StringComparison.OrdinalIgnoreCase))) >= 3);
}
