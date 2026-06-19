using System.Collections.Frozen;
using System.Text;
using Reva.Core.Documents;

namespace Reva.Infrastructure.Agent;

internal static class AgentRoutes
{
    public const string Dashboard = "dashboard";
    public const string Review = "review";
    public const string Mappings = "mappings";
    public const string Export = "export";
    public const string Settings = "settings";

    public static readonly FrozenSet<string> All =
        new[] { Dashboard, Review, Mappings, Export, Settings }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}

internal static class AgentReviewDecisions
{
    public const string Approved = ReviewDecisionMapper.Approved;
    public const string Rejected = ReviewDecisionMapper.Rejected;
    public const string NeedsCorrection = ReviewDecisionMapper.NeedsCorrection;

    public const string Reviewer = "agent";

    public static readonly FrozenDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["approve"] = Approved,
        ["approved"] = Approved,
        ["accept"] = Approved,
        ["reject"] = Rejected,
        ["rejected"] = Rejected,
        ["decline"] = Rejected,
        ["needscorrection"] = NeedsCorrection,
        ["correction"] = NeedsCorrection,
        ["correct"] = NeedsCorrection
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}

internal static class AgentToolNames
{
    public const string Goto = "goto";
    public const string OpenDocument = "open_document";
    public const string RefreshQueue = "refresh_queue";
    public const string CorrectField = "correct_field";
    public const string SetReviewState = "set_review_state";
    public const string ExportDocument = "export_document";
    public const string FilterQueue = "filter_queue";
    public const string Reseed = "reseed";
    public const string Clear = "clear";
    public const string SearchKnowledge = "search_knowledge";
}

internal static class AgentToolMessages
{
    public const string DocumentNotFound = "Document not found.";
    public const string DocumentReferenceNotFound = "Document not found or ambiguous. Use the full file name.";
    public const string InvalidDocumentId = "The document file name or id is not a valid identifier.";
    public const string UnknownRoute = "Unknown route. Valid routes are dashboard, review, mappings, export, settings.";
    public const string FieldRequired = "A field name is required.";
    public const string UnknownDecision = "Unknown decision. Use approve, reject, or needscorrection.";
    public const string ConfirmationRequired = "This is a destructive action and requires confirm=true to proceed.";
    public const string MaintenanceUnavailable = "Data maintenance is not available in this host.";
    public const string Processing = "Processing documents…";
    public const string FieldCorrected = "Field '{0}' updated for {1}.";
    public const string ReviewStateUpdated = "Review state for {0} set to {1}.";
    public const string Exported = "Exported {0} as {1} to {2}.";
    public const string UnsupportedExportFormat = "Unsupported export format. Use csv, xlsx, or json.";
    public const string FilterApplied = "Requested queue filter '{0}'.";
    public const string QueueState = "{0} document(s) in the queue.";
    public const string Reseeded = "Demo corpus reseeded.";
    public const string AlreadySeeded = "Workspace is not empty; nothing was reseeded.";
    public const string Cleared = "Cleared {0} document(s).";
}

internal static class AgentToolMessageFormats
{
    public static readonly CompositeFormat FieldCorrected = CompositeFormat.Parse(AgentToolMessages.FieldCorrected);
    public static readonly CompositeFormat ReviewStateUpdated = CompositeFormat.Parse(AgentToolMessages.ReviewStateUpdated);
    public static readonly CompositeFormat Exported = CompositeFormat.Parse(AgentToolMessages.Exported);
    public static readonly CompositeFormat FilterApplied = CompositeFormat.Parse(AgentToolMessages.FilterApplied);
    public static readonly CompositeFormat QueueState = CompositeFormat.Parse(AgentToolMessages.QueueState);
    public static readonly CompositeFormat Cleared = CompositeFormat.Parse(AgentToolMessages.Cleared);
}
