using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Reva.App.Navigation;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using SkiaSharp;

namespace Reva.App.ViewModels;

public sealed record DashboardRow(
    Guid Id,
    string FileName,
    string DocumentType,
    string Status,
    string Confidence,
    int Exceptions,
    string Updated);

public sealed record PipelineStage(string Label, int Count, double Fraction, IBrush Accent);

public partial class DashboardViewModel : ViewModelBase, IRefreshable
{
    public Task RefreshAsync() => LoadAsync(CancellationToken.None);

    private const int RecentDocumentLimit = 8;
    private const int ThroughputDayWindow = 7;
    private const string EmptyMetric = "—";

    private const string TokenPrimary = "PrimaryBrush";
    private const string TokenAccent = "AccentBrush";
    private const string TokenSuccess = "SuccessBrush";
    private const string TokenWarning = "WarningBrush";
    private const string TokenBorder = "BorderBrush";
    private const string TokenMutedForeground = "MutedForegroundBrush";

    private static readonly SKColor FallbackPrimary = new(0x4F, 0x7D, 0xF9);
    private static readonly SKColor FallbackWarning = new(0xF5, 0x9E, 0x0B);
    private static readonly SKColor FallbackGrid = new(0x24, 0x30, 0x4A);
    private static readonly SKColor FallbackMuted = new(0x8A, 0x95, 0xAD);

    private readonly IRevaClient _client;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _title = "Dashboard";

    [ObservableProperty]
    private string _description = "Portfolio overview, ingestion throughput, and review queue health.";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasDocuments;

    [ObservableProperty]
    private string _emptyStateMessage = "No documents yet — drop files to begin.";

    [ObservableProperty]
    private int _totalDocuments;

    [ObservableProperty]
    private int _needsReviewCount;

    [ObservableProperty]
    private int _exceptionCount;

    [ObservableProperty]
    private string _averageConfidence = EmptyMetric;

    [ObservableProperty]
    private int _uploadedCount;

    [ObservableProperty]
    private int _extractedCount;

    [ObservableProperty]
    private int _reviewedCount;

    [ObservableProperty]
    private int _exportedCount;

    [ObservableProperty]
    private ISeries[] _throughputSeries = [];

    [ObservableProperty]
    private Axis[] _throughputXAxes = [];

    [ObservableProperty]
    private Axis[] _throughputYAxes = [];

    [ObservableProperty]
    private ISeries[] _exceptionSeries = [];

    [ObservableProperty]
    private Axis[] _exceptionXAxes = [];

    [ObservableProperty]
    private Axis[] _exceptionYAxes = [];

    [ObservableProperty]
    private bool _hasThroughput;

    [ObservableProperty]
    private bool _hasExceptionBreakdown;

    public DashboardViewModel(IRevaClient client, INavigationService navigation)
    {
        _client = client;
        _navigation = navigation;
        ApplyEmptyState();
    }

    public ObservableCollection<PipelineStage> PipelineStages { get; } = [];

    public ObservableCollection<DashboardRow> RecentDocuments { get; } = [];

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var documents = await _client.ListDocumentsAsync(cancellationToken);
            Project(documents ?? []);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            Project([]);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenDocument(DashboardRow? row)
    {
        if (row is null)
        {
            return;
        }

        _navigation.OpenDocument(row.Id);
    }

    private void Project(IReadOnlyList<DocumentSummary> documents)
    {
        TotalDocuments = documents.Count;
        HasDocuments = documents.Count > 0;

        NeedsReviewCount = documents.Count(static d => d.ReviewState is ReviewState.Pending or ReviewState.NeedsCorrection);
        ExceptionCount = documents.Sum(static d => Math.Max(0, d.ExceptionCount));
        AverageConfidence = FormatAverageConfidence(documents);

        BuildPipeline(documents);
        BuildThroughput(documents);
        BuildExceptionBreakdown(documents);
        BuildRecent(documents);
    }

    private void BuildPipeline(IReadOnlyList<DocumentSummary> documents)
    {
        var uploaded = documents.Count;
        var extracted = documents.Count(static d => d.Status is DocumentStatus.Extracted);
        var reviewed = documents.Count(static d => d.ReviewState is ReviewState.Approved or ReviewState.Rejected);
        var exported = documents.Count(static d => d.ReviewState is ReviewState.Approved);

        UploadedCount = uploaded;
        ExtractedCount = extracted;
        ReviewedCount = reviewed;
        ExportedCount = exported;

        var max = Math.Max(1, uploaded);

        PipelineStages.Clear();
        PipelineStages.Add(new PipelineStage("Uploaded", uploaded, uploaded / (double)max, ResolveBrush(TokenPrimary)));
        PipelineStages.Add(new PipelineStage("Extracted", extracted, extracted / (double)max, ResolveBrush(TokenAccent)));
        PipelineStages.Add(new PipelineStage("Reviewed", reviewed, reviewed / (double)max, ResolveBrush(TokenSuccess)));
        PipelineStages.Add(new PipelineStage("Exported", exported, exported / (double)max, ResolveBrush(TokenWarning)));
    }

    private void BuildThroughput(IReadOnlyList<DocumentSummary> documents)
    {
        var today = DateTimeOffset.Now.Date;
        var labels = new string[ThroughputDayWindow];
        var counts = new double[ThroughputDayWindow];

        for (var index = 0; index < ThroughputDayWindow; index++)
        {
            var day = today.AddDays(index - (ThroughputDayWindow - 1));
            labels[index] = day.ToString("ddd", CultureInfo.CurrentCulture);
            counts[index] = documents.Count(d => d.CreatedAt.ToLocalTime().Date == day);
        }

        HasThroughput = counts.Any(static c => c > 0);

        ThroughputSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Documents",
                Values = counts,
                Fill = new SolidColorPaint(ResolveColor(TokenPrimary, FallbackPrimary)),
                Stroke = null,
                MaxBarWidth = 28,
                Rx = 4,
                Ry = 4
            }
        ];

        ThroughputXAxes = [BuildLabelAxis(labels)];
        ThroughputYAxes = [BuildValueAxis()];
    }

    private void BuildExceptionBreakdown(IReadOnlyList<DocumentSummary> documents)
    {
        var buckets = new (string Label, Func<DocumentSummary, bool> Predicate)[]
        {
            ("Pending", static d => d.ReviewState is ReviewState.Pending),
            ("Needs fix", static d => d.ReviewState is ReviewState.NeedsCorrection),
            ("Rejected", static d => d.ReviewState is ReviewState.Rejected),
            ("Unsupported", static d => d.Status is DocumentStatus.Unsupported),
            ("Failed", static d => d.Status is DocumentStatus.Failed)
        };

        var labels = new List<string>(buckets.Length);
        var values = new List<double>(buckets.Length);

        foreach (var bucket in buckets)
        {
            var count = documents.Count(d => bucket.Predicate(d));
            if (count <= 0)
            {
                continue;
            }

            labels.Add(bucket.Label);
            values.Add(count);
        }

        HasExceptionBreakdown = values.Count > 0;
        ExceptionSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Open",
                Values = [.. values],
                Fill = new SolidColorPaint(ResolveColor(TokenWarning, FallbackWarning)),
                Stroke = null,
                MaxBarWidth = 48,
                Rx = 4,
                Ry = 4
            }
        ];
        ExceptionXAxes = [BuildLabelAxis([.. labels])];
        ExceptionYAxes = [BuildValueAxis()];
    }

    private void BuildRecent(IReadOnlyList<DocumentSummary> documents)
    {
        RecentDocuments.Clear();
        foreach (var document in documents
                     .OrderByDescending(static d => d.UpdatedAt)
                     .Take(RecentDocumentLimit))
        {
            RecentDocuments.Add(new DashboardRow(
                document.Id,
                document.FileName,
                FormatDocumentType(document.DocumentType),
                FormatStatus(document.Status, document.ReviewState),
                FormatConfidence(document.Confidence),
                Math.Max(0, document.ExceptionCount),
                FormatUpdated(document.UpdatedAt)));
        }
    }

    private void ApplyEmptyState()
    {
        TotalDocuments = 0;
        NeedsReviewCount = 0;
        ExceptionCount = 0;
        AverageConfidence = EmptyMetric;
        UploadedCount = 0;
        ExtractedCount = 0;
        ReviewedCount = 0;
        ExportedCount = 0;
        HasDocuments = false;
        HasThroughput = false;
        HasExceptionBreakdown = false;
        PipelineStages.Clear();
        RecentDocuments.Clear();
        ThroughputSeries = [];
        ThroughputXAxes = [];
        ThroughputYAxes = [];
        ExceptionSeries = [];
        ExceptionXAxes = [];
        ExceptionYAxes = [];
    }

    private static Axis BuildLabelAxis(IReadOnlyList<string> labels) => new()
    {
        Labels = [.. labels],
        LabelsPaint = new SolidColorPaint(ResolveColor(TokenMutedForeground, FallbackMuted)),
        TextSize = 12,
        SeparatorsPaint = null,
        TicksPaint = null,
        MinStep = 1,
        ForceStepToMin = true
    };

    private static Axis BuildValueAxis() => new()
    {
        MinLimit = 0,
        LabelsPaint = new SolidColorPaint(ResolveColor(TokenMutedForeground, FallbackMuted)),
        TextSize = 12,
        SeparatorsPaint = new SolidColorPaint(ResolveColor(TokenBorder, FallbackGrid)) { StrokeThickness = 1 },
        MinStep = 1
    };

    private static string FormatAverageConfidence(IReadOnlyList<DocumentSummary> documents)
    {
        if (documents.Count == 0)
        {
            return EmptyMetric;
        }

        var average = documents.Average(static d => Clamp01(d.Confidence));
        return FormatConfidence(average);
    }

    private static string FormatConfidence(double confidence) =>
        Clamp01(confidence).ToString("P0", CultureInfo.CurrentCulture);

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0d;
        }

        return Math.Clamp(value, 0d, 1d);
    }

    private static string FormatDocumentType(ReinsuranceDocumentType type) => type switch
    {
        ReinsuranceDocumentType.FacultativeSlip => "Facultative slip",
        ReinsuranceDocumentType.StatementOfAccount => "Statement of account",
        ReinsuranceDocumentType.LossRun => "Loss run",
        ReinsuranceDocumentType.ClaimNotice => "Claim notice",
        ReinsuranceDocumentType.Unknown => "Unclassified",
        _ => type.ToString()
    };

    private static string FormatStatus(DocumentStatus status, ReviewState reviewState)
    {
        if (status is DocumentStatus.Unsupported)
        {
            return "Unsupported";
        }

        if (status is DocumentStatus.Failed)
        {
            return "Failed";
        }

        return reviewState switch
        {
            ReviewState.Approved => "Approved",
            ReviewState.Rejected => "Rejected",
            ReviewState.NeedsCorrection => "Needs correction",
            _ => "Pending review"
        };
    }

    private static string FormatUpdated(DateTimeOffset updatedAt)
    {
        var delta = DateTimeOffset.Now - updatedAt;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        if (delta < TimeSpan.FromMinutes(1))
        {
            return "Just now";
        }

        if (delta < TimeSpan.FromHours(1))
        {
            return $"{(int)delta.TotalMinutes}m ago";
        }

        if (delta < TimeSpan.FromDays(1))
        {
            return $"{(int)delta.TotalHours}h ago";
        }

        if (delta < TimeSpan.FromDays(7))
        {
            return $"{(int)delta.TotalDays}d ago";
        }

        return updatedAt.ToLocalTime().ToString("d MMM", CultureInfo.CurrentCulture);
    }

    private static IBrush ResolveBrush(string token)
    {
        if (TryGetResource(token, out var resource) && resource is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    private static SKColor ResolveColor(string token, SKColor fallback)
    {
        if (TryGetResource(token, out var resource) &&
            resource is ISolidColorBrush brush)
        {
            var color = brush.Color;
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        return fallback;
    }

    private static bool TryGetResource(string token, out object? resource)
    {
        resource = null;
        if (Application.Current is not { } application)
        {
            return false;
        }

        var theme = application.ActualThemeVariant ?? ThemeVariant.Dark;
        return application.TryGetResource(token, theme, out resource);
    }
}
