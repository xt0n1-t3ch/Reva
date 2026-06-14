using System.Globalization;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using Reva.Core.Settings;

namespace Reva.Web.Components.Services;

public static class CockpitFormat
{
    public static string Percent(double confidence) =>
        (confidence * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    public static string PercentWhole(double confidence) =>
        Math.Round(confidence * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    // Tier thresholds are user-configurable in Settings (RuntimeSettings).
    public static string ConfidenceLevel(double confidence)
    {
        var settings = RuntimeSettings.Current;
        if (confidence < settings.ConfidenceLowMax)
        {
            return "lvl-low";
        }

        return confidence < settings.ConfidenceMediumMax ? "lvl-mid" : "lvl-high";
    }

    public static string StatusTone(DocumentStatus status) => status switch
    {
        DocumentStatus.Extracted => "tone-green",
        DocumentStatus.Parsed => "tone-blue",
        DocumentStatus.Uploaded => "tone-slate",
        DocumentStatus.Unsupported => "tone-amber",
        DocumentStatus.Failed => "tone-red",
        _ => "tone-slate"
    };

    public static bool IsActionable(DocumentStatus status) =>
        status == DocumentStatus.Extracted;

    public static string ReviewTone(ReviewState state) => state switch
    {
        ReviewState.Approved => "tone-green",
        ReviewState.Pending => "tone-amber",
        ReviewState.Rejected => "tone-red",
        ReviewState.NeedsCorrection => "tone-violet",
        _ => "tone-slate"
    };

    public static string ReviewLabel(ReviewState state) => state switch
    {
        ReviewState.NeedsCorrection => "Needs correction",
        _ => state.ToString()
    };

    public static string DocumentTypeLabel(ReinsuranceDocumentType type) => type switch
    {
        ReinsuranceDocumentType.FacultativeSlip => "Facultative slip",
        ReinsuranceDocumentType.StatementOfAccount => "Statement of account",
        ReinsuranceDocumentType.LossRun => "Loss run",
        ReinsuranceDocumentType.ClaimNotice => "Claim notice",
        _ => type.ToString()
    };

    public static string SeverityLabel(ExceptionSeverity severity) => severity switch
    {
        ExceptionSeverity.Critical => "Critical",
        ExceptionSeverity.Warning => "Warning",
        _ => "Note"
    };

    // Reconciliation agreement tier from a [0,1] score: Low (red), Medium (amber), High (green).
    // Thresholds are user-configurable in Settings.
    public static string AgreementTier(double score)
    {
        var settings = RuntimeSettings.Current;
        if (score < settings.ConfidenceLowMax)
        {
            return "tier-low";
        }

        return score < settings.ConfidenceMediumMax ? "tier-mid" : "tier-high";
    }

    public static string AgreementLabel(double score)
    {
        var settings = RuntimeSettings.Current;
        if (score < settings.ConfidenceLowMax)
        {
            return "Low";
        }

        return score < settings.ConfidenceMediumMax ? "Medium" : "High";
    }

    public static string SeverityClass(ExceptionSeverity severity) => severity switch
    {
        ExceptionSeverity.Critical => "sev-critical",
        ExceptionSeverity.Warning => "sev-warning",
        _ => "sev-info"
    };

    public static string SeverityTone(ExceptionSeverity severity) => severity switch
    {
        ExceptionSeverity.Critical => "tone-red",
        ExceptionSeverity.Warning => "tone-amber",
        _ => "tone-blue"
    };

    public static string ExceptionTone(int count) => count switch
    {
        0 => "tone-green",
        <= 2 => "tone-amber",
        _ => "tone-red"
    };

    public static string RelativeTime(DateTimeOffset moment, DateTimeOffset now)
    {
        var delta = now - moment;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        if (delta.TotalMinutes < 1)
        {
            return "just now";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes}m ago";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours}h ago";
        }

        if (delta.TotalDays < 30)
        {
            return $"{(int)delta.TotalDays}d ago";
        }

        return moment.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }
}
