namespace Reva.Core.Documents;

public static class ReviewDecisionMapper
{
    public const string Approved = nameof(ReviewState.Approved);
    public const string Rejected = nameof(ReviewState.Rejected);
    public const string NeedsCorrection = nameof(ReviewState.NeedsCorrection);

    public static ReviewState ToReviewState(string? decision)
    {
        return decision?.Trim() switch
        {
            Approved => ReviewState.Approved,
            Rejected => ReviewState.Rejected,
            NeedsCorrection => ReviewState.NeedsCorrection,
            "Approve" => ReviewState.Approved,
            "Reject" => ReviewState.Rejected,
            "RequestCorrection" => ReviewState.NeedsCorrection,
            "MappingCorrection" => ReviewState.NeedsCorrection,
            _ => ReviewState.NeedsCorrection
        };
    }
}
