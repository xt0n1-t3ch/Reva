using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Infrastructure.Agent;

namespace Reva.App.ViewModels;

public sealed partial class CitationRegion : ObservableObject
{
    private const double IdleStrokeThickness = 1.5d;
    private const double ActiveStrokeThickness = 2.5d;

    public CitationRegion(string fieldKey, int page, SourceBox box, string? quote)
    {
        FieldKey = fieldKey;
        Page = page;
        NormalizedX = Clamp01(box.X);
        NormalizedY = Clamp01(box.Y);
        NormalizedWidth = Clamp01(box.Width);
        NormalizedHeight = Clamp01(box.Height);
        Quote = quote ?? string.Empty;
    }

    public string FieldKey { get; }

    public int Page { get; }

    public double NormalizedX { get; }

    public double NormalizedY { get; }

    public double NormalizedWidth { get; }

    public double NormalizedHeight { get; }

    public string Quote { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrokeBrush))]
    [NotifyPropertyChangedFor(nameof(FillBrush))]
    [NotifyPropertyChangedFor(nameof(StrokeThickness))]
    private bool _isActive;

    [ObservableProperty]
    private double _left;

    [ObservableProperty]
    private double _top;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    public IBrush StrokeBrush => ReviewBrushPalette.Resolve(IsActive ? ReviewBrushKeys.Accent : ReviewBrushKeys.Primary);

    public IBrush FillBrush => ReviewBrushPalette.ResolveCitationFill(IsActive);

    public double StrokeThickness => IsActive ? ActiveStrokeThickness : IdleStrokeThickness;

    public void Project(double surfaceWidth, double surfaceHeight)
    {
        Left = NormalizedX * surfaceWidth;
        Top = NormalizedY * surfaceHeight;
        Width = Math.Max(0d, NormalizedWidth * surfaceWidth);
        Height = Math.Max(0d, NormalizedHeight * surfaceHeight);
    }

    private static double Clamp01(double value) => double.IsNaN(value) ? 0d : Math.Clamp(value, 0d, 1d);
}

public sealed partial class ReviewFieldItem : ObservableObject
{
    private readonly Action<ReviewFieldItem> _onHoverChanged;

    public ReviewFieldItem(FieldValue field, double lowMax, double mediumMax, Action<ReviewFieldItem> onHoverChanged)
    {
        ArgumentNullException.ThrowIfNull(field);
        _onHoverChanged = onHoverChanged ?? (static _ => { });
        Key = field.Key;
        Label = string.IsNullOrWhiteSpace(field.Label) ? field.Key : field.Label;
        OriginalValue = field.Value ?? string.Empty;
        _editedValue = OriginalValue;
        Status = field.Status ?? string.Empty;
        Confidence = Math.Clamp(field.Confidence, 0d, 1d);
        IsReviewed = string.Equals(Status, ReviewFieldStatuses.UserConfirmed, StringComparison.OrdinalIgnoreCase);
        ConfidenceLabel = Confidence.ToString("P0", CultureInfo.CurrentCulture);
        ConfidenceTier = ResolveTier(Confidence, Status, lowMax, mediumMax);
        ConfidenceBrush = ReviewBrushPalette.Resolve(ResolveConfidenceBrushKey(ConfidenceTier));
        StatusLabel = ResolveStatusLabel(Status);
    }

    public string Key { get; }

    public string Label { get; }

    public string OriginalValue { get; }

    public string Status { get; }

    public double Confidence { get; }

    public string ConfidenceLabel { get; }

    public string ConfidenceTier { get; }

    public IBrush ConfidenceBrush { get; }

    public string StatusLabel { get; }

    public bool HasCitations { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEdited))]
    private string _editedValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HighlightBrush))]
    private bool _isHighlighted;

    [ObservableProperty]
    private bool _isReviewed;

    public bool IsEdited => !string.Equals(EditedValue ?? string.Empty, OriginalValue, StringComparison.Ordinal);

    public IBrush HighlightBrush => ReviewBrushPalette.Resolve(IsHighlighted ? ReviewBrushKeys.Surface2 : ReviewBrushKeys.Surface);

    public void MarkHasCitations(bool value) => HasCitations = value;

    public FieldCorrection? ToCorrection() =>
        IsEdited ? new FieldCorrection(Key, EditedValue ?? string.Empty) : null;

    [RelayCommand]
    private void HoverEnter()
    {
        IsHighlighted = true;
        _onHoverChanged(this);
    }

    [RelayCommand]
    private void HoverLeave()
    {
        IsHighlighted = false;
        _onHoverChanged(this);
    }

    private static string ResolveTier(double confidence, string status, double lowMax, double mediumMax)
    {
        if (string.Equals(status, ReviewFieldStatuses.Missing, StringComparison.OrdinalIgnoreCase))
        {
            return ReviewConfidenceTiers.Missing;
        }

        if (string.Equals(status, ReviewFieldStatuses.UserConfirmed, StringComparison.OrdinalIgnoreCase))
        {
            return ReviewConfidenceTiers.High;
        }

        if (confidence <= lowMax)
        {
            return ReviewConfidenceTiers.Low;
        }

        return confidence <= mediumMax ? ReviewConfidenceTiers.Medium : ReviewConfidenceTiers.High;
    }

    private static string ResolveConfidenceBrushKey(string tier) => tier switch
    {
        ReviewConfidenceTiers.High => ReviewBrushKeys.Success,
        ReviewConfidenceTiers.Medium => ReviewBrushKeys.Warning,
        ReviewConfidenceTiers.Low => ReviewBrushKeys.Danger,
        _ => ReviewBrushKeys.Muted
    };

    private static string ResolveStatusLabel(string status) => status switch
    {
        ReviewFieldStatuses.UserConfirmed => "Reviewed",
        ReviewFieldStatuses.Detected => "Detected",
        ReviewFieldStatuses.LowConfidence => "Low confidence",
        ReviewFieldStatuses.Missing => "Missing",
        _ => string.IsNullOrWhiteSpace(status) ? "Pending" : status
    };
}

public sealed partial class ReconciliationItem : ObservableObject
{
    public ReconciliationItem(ReconciliationCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        Name = string.IsNullOrWhiteSpace(check.Name) ? check.Id : check.Name;
        ExpectedValue = string.IsNullOrWhiteSpace(check.Expected.Value) ? ReviewDisplay.EmptyValue : check.Expected.Value;
        DetectedValue = string.IsNullOrWhiteSpace(check.Detected.Value) ? ReviewDisplay.EmptyValue : check.Detected.Value;
        DeltaLabel = FormatDelta(check.Delta);
        Explanation = check.Explanation ?? string.Empty;
        StatusLabel = ResolveStatusLabel(check.Status);
        StatusBrush = ReviewBrushPalette.Resolve(ResolveStatusBrushKey(check.Status));
    }

    public string Name { get; }

    public string ExpectedValue { get; }

    public string DetectedValue { get; }

    public string DeltaLabel { get; }

    public string Explanation { get; }

    public string StatusLabel { get; }

    public IBrush StatusBrush { get; }

    public bool HasExplanation => !string.IsNullOrWhiteSpace(Explanation);

    private static string FormatDelta(double delta)
    {
        if (double.IsNaN(delta) || delta == 0d)
        {
            return ReviewDisplay.NoDelta;
        }

        var prefix = delta > 0d ? "+" : string.Empty;
        return string.Concat(prefix, delta.ToString("0.##", CultureInfo.CurrentCulture));
    }

    private static string ResolveStatusLabel(string status) => status switch
    {
        ReviewReconciliationStatuses.Pass => "Reconciled",
        ReviewReconciliationStatuses.Fail => "Break",
        ReviewReconciliationStatuses.Warning => "Warning",
        _ => string.IsNullOrWhiteSpace(status) ? "Unknown" : status
    };

    private static string ResolveStatusBrushKey(string status) => status switch
    {
        ReviewReconciliationStatuses.Pass => ReviewBrushKeys.Success,
        ReviewReconciliationStatuses.Fail => ReviewBrushKeys.Danger,
        ReviewReconciliationStatuses.Warning => ReviewBrushKeys.Warning,
        _ => ReviewBrushKeys.Muted
    };
}

public sealed partial class DocumentPickerItem : ObservableObject
{
    public DocumentPickerItem(DocumentSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        Id = summary.Id;
        FileName = string.IsNullOrWhiteSpace(summary.FileName) ? summary.Id.ToString() : summary.FileName;
        TypeLabel = summary.DocumentType.ToString();
        StateLabel = ResolveStateLabel(summary.ReviewState);
        StateBrush = ReviewBrushPalette.Resolve(ResolveStateBrushKey(summary.ReviewState));
        ExceptionLabel = summary.ExceptionCount == 1 ? "1 exception" : $"{summary.ExceptionCount} exceptions";
        HasExceptions = summary.ExceptionCount > 0;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string TypeLabel { get; }

    public string StateLabel { get; }

    public IBrush StateBrush { get; }

    public string ExceptionLabel { get; }

    public bool HasExceptions { get; }

    private static string ResolveStateLabel(ReviewState state) => state switch
    {
        ReviewState.Approved => "Approved",
        ReviewState.Rejected => "Rejected",
        ReviewState.NeedsCorrection => "Needs correction",
        _ => "Pending"
    };

    private static string ResolveStateBrushKey(ReviewState state) => state switch
    {
        ReviewState.Approved => ReviewBrushKeys.Success,
        ReviewState.Rejected => ReviewBrushKeys.Danger,
        ReviewState.NeedsCorrection => ReviewBrushKeys.Warning,
        _ => ReviewBrushKeys.Muted
    };
}

public partial class ReviewViewModel : ViewModelBase, IDocumentNavigationTarget, IDisposable
{
    private const double LowConfidenceMax = 0.6d;
    private const double MediumConfidenceMax = 0.85d;

    private readonly IRevaClient _client;
    private readonly IAppActionBus _actionBus;
    private readonly IDisposable? _actionSubscription;

    private CancellationTokenSource? _loadCts;
    private BdxReviewPayload? _payload;
    private double _surfaceWidth;
    private double _surfaceHeight;
    private bool _disposed;
    private bool _isSyncingSelection;

    [ObservableProperty]
    private string _title = "Review";

    [ObservableProperty]
    private string _description = "Side-by-side document review with citations, reconciliation, and corrections.";

    [ObservableProperty]
    private Guid? _requestedDocumentId;

    [ObservableProperty]
    private DocumentPickerItem? _selectedDocument;

    [ObservableProperty]
    private Bitmap? _pageImage;

    [ObservableProperty]
    private double _pageImageWidth;

    [ObservableProperty]
    private double _pageImageHeight;

    [ObservableProperty]
    private int _currentPage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasDocument;

    [ObservableProperty]
    private bool _hasPageImage;

    [ObservableProperty]
    private bool _hasFields;

    [ObservableProperty]
    private bool _hasReconciliation;

    [ObservableProperty]
    private bool _hasCitations;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RejectCommand))]
    [NotifyCanExecuteChangedFor(nameof(NeedsCorrectionCommand))]
    private bool _canReview;

    [ObservableProperty]
    private string _reviewerNotes = string.Empty;

    public ReviewViewModel(IRevaClient client, IAppActionBus actionBus)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _actionBus = actionBus ?? throw new ArgumentNullException(nameof(actionBus));
        Documents = [];
        Fields = [];
        Reconciliation = [];
        Citations = [];
        _actionSubscription = _actionBus.Actions.Subscribe(new ActionObserver(this));
    }

    public ObservableCollection<DocumentPickerItem> Documents { get; }

    public ObservableCollection<ReviewFieldItem> Fields { get; }

    public ObservableCollection<ReconciliationItem> Reconciliation { get; }

    public ObservableCollection<CitationRegion> Citations { get; }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public void RequestDocument(Guid documentId)
    {
        RequestedDocumentId = documentId;
        _ = SelectDocumentAsync(documentId);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await LoadDocumentsAsync(cancellationToken).ConfigureAwait(true);
            var target = RequestedDocumentId ?? Documents.FirstOrDefault()?.Id;
            if (target is { } documentId)
            {
                await SelectDocumentAsync(documentId, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                ClearDocument();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ReviewDisplay.LoadFailed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdateSurface(double width, double height)
    {
        _surfaceWidth = double.IsNaN(width) || width < 0d ? 0d : width;
        _surfaceHeight = double.IsNaN(height) || height < 0d ? 0d : height;
        ProjectCitations();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _actionSubscription?.Dispose();
        CancelPendingLoad();
        PageImage?.Dispose();
        PageImage = null;
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanReview))]
    private Task ApproveAsync(CancellationToken cancellationToken) =>
        SubmitDecisionAsync(ReviewDecisions.Approve, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanReview))]
    private Task RejectAsync(CancellationToken cancellationToken) =>
        SubmitDecisionAsync(ReviewDecisions.Reject, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanReview))]
    private Task NeedsCorrectionAsync(CancellationToken cancellationToken) =>
        SubmitDecisionAsync(ReviewDecisions.NeedsCorrection, cancellationToken);

    partial void OnSelectedDocumentChanged(DocumentPickerItem? value)
    {
        if (_isSyncingSelection || value is null || value.Id == _payload?.Document.Id)
        {
            return;
        }

        _ = SelectDocumentAsync(value.Id);
    }

    private async Task LoadDocumentsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DocumentSummary> summaries;
        try
        {
            summaries = await _client.ListDocumentsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            summaries = [];
        }

        Documents.Clear();
        foreach (var summary in summaries)
        {
            Documents.Add(new DocumentPickerItem(summary));
        }
    }

    private async Task SelectDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        CancelPendingLoad();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadCts = cts;
        var token = cts.Token;

        IsBusy = true;
        SetStatus(string.Empty);
        try
        {
            var payload = await _client.GetReviewPayloadAsync(documentId, token).ConfigureAwait(true);
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (payload is null)
            {
                ClearDocument();
                SetStatus(ReviewDisplay.DocumentMissing);
                return;
            }

            ApplyPayload(payload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ClearDocument();
            SetStatus(ReviewDisplay.LoadFailed);
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
            {
                _loadCts = null;
                IsBusy = false;
            }

            cts.Dispose();
        }
    }

    private void ApplyPayload(BdxReviewPayload payload)
    {
        _payload = payload;
        RequestedDocumentId = payload.Document.Id;
        Title = string.IsNullOrWhiteSpace(payload.Document.Filename) ? "Review" : payload.Document.Filename;
        SyncSelectedDocument(payload.Document.Id);

        var lowMax = LowConfidenceMax;
        var mediumMax = MediumConfidenceMax;

        Fields.Clear();
        foreach (var field in payload.Fields)
        {
            var item = new ReviewFieldItem(field, lowMax, mediumMax, OnFieldHoverChanged);
            item.MarkHasCitations(field.Provenance.Citations.Count > 0);
            Fields.Add(item);
        }

        Reconciliation.Clear();
        foreach (var check in payload.Reconciliation)
        {
            Reconciliation.Add(new ReconciliationItem(check));
        }

        HasFields = Fields.Count > 0;
        HasReconciliation = Reconciliation.Count > 0;
        HasDocument = true;
        CanReview = true;
        ReviewerNotes = string.Empty;

        var firstPage = payload.Document.Pages.Count > 0 ? payload.Document.Pages[0].Page : 1;
        LoadPage(firstPage);
    }

    private void LoadPage(int page)
    {
        CurrentPage = page;
        BuildCitations(page);
        LoadPageImage(page);
    }

    private void BuildCitations(int page)
    {
        Citations.Clear();
        if (_payload is null)
        {
            HasCitations = false;
            return;
        }

        foreach (var field in _payload.Fields)
        {
            foreach (var citation in field.Provenance.Citations)
            {
                if (citation.Page != page)
                {
                    continue;
                }

                Citations.Add(new CitationRegion(field.Key, citation.Page, citation.Bbox, citation.Quote));
            }
        }

        HasCitations = Citations.Count > 0;
        ProjectCitations();
    }

    private void LoadPageImage(int page)
    {
        DisposePageImage();

        var pageInfo = _payload?.Document.Pages.FirstOrDefault(candidate => candidate.Page == page);
        var resolvedPath = ReviewImageResolver.Resolve(pageInfo?.ImageUrl);
        if (resolvedPath is null)
        {
            HasPageImage = false;
            return;
        }

        try
        {
            var bitmap = new Bitmap(resolvedPath);
            PageImage = bitmap;
            PageImageWidth = bitmap.Size.Width;
            PageImageHeight = bitmap.Size.Height;
            HasPageImage = true;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException or UnauthorizedAccessException)
        {
            HasPageImage = false;
        }
    }

    private void ProjectCitations()
    {
        if (_surfaceWidth <= 0d || _surfaceHeight <= 0d)
        {
            return;
        }

        foreach (var citation in Citations)
        {
            citation.Project(_surfaceWidth, _surfaceHeight);
        }
    }

    private void OnFieldHoverChanged(ReviewFieldItem field)
    {
        SetActiveCitations(field.IsHighlighted ? field.Key : null);
    }

    private void SetActiveCitations(string? fieldKey)
    {
        foreach (var citation in Citations)
        {
            citation.IsActive = fieldKey is not null && string.Equals(citation.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void HighlightField(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            SetActiveCitations(null);
            foreach (var field in Fields)
            {
                field.IsHighlighted = false;
            }

            return;
        }

        ReviewFieldItem? matched = null;
        foreach (var field in Fields)
        {
            var isMatch = string.Equals(field.Key, target, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Label, target, StringComparison.OrdinalIgnoreCase);
            field.IsHighlighted = isMatch;
            if (isMatch)
            {
                matched = field;
            }
        }

        SetActiveCitations(matched?.Key);
    }

    private async Task SubmitDecisionAsync(string decision, CancellationToken cancellationToken)
    {
        if (_payload is null)
        {
            return;
        }

        IsBusy = true;
        CanReview = false;
        try
        {
            var corrections = Fields
                .Select(field => field.ToCorrection())
                .Where(correction => correction is not null)
                .Select(correction => correction!)
                .ToList();
            var notes = string.IsNullOrWhiteSpace(ReviewerNotes) ? null : ReviewerNotes.Trim();
            var reviewDecision = new ReviewDecision(decision, ReviewDecisions.Reviewer, notes, corrections);
            var updated = await _client.SaveReviewAsync(_payload.Document.Id, reviewDecision, cancellationToken).ConfigureAwait(true);
            if (updated is null)
            {
                SetStatus(ReviewDisplay.SaveFailed);
                return;
            }

            SetStatus(ResolveDecisionConfirmation(decision));
            await LoadDocumentsAsync(cancellationToken).ConfigureAwait(true);
            await SelectDocumentAsync(_payload.Document.Id, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ReviewDisplay.SaveFailed);
        }
        finally
        {
            CanReview = _payload is not null;
            IsBusy = false;
        }
    }

    private void SyncSelectedDocument(Guid documentId)
    {
        var match = Documents.FirstOrDefault(document => document.Id == documentId);
        if (match is null || ReferenceEquals(SelectedDocument, match))
        {
            return;
        }

        _isSyncingSelection = true;
        try
        {
            SelectedDocument = match;
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void ClearDocument()
    {
        _payload = null;
        DisposePageImage();
        Fields.Clear();
        Reconciliation.Clear();
        Citations.Clear();
        HasDocument = false;
        HasFields = false;
        HasReconciliation = false;
        HasCitations = false;
        HasPageImage = false;
        CanReview = false;
        CurrentPage = 0;
        Title = "Review";
    }

    private void DisposePageImage()
    {
        var existing = PageImage;
        PageImage = null;
        PageImageWidth = 0d;
        PageImageHeight = 0d;
        existing?.Dispose();
    }

    private void CancelPendingLoad()
    {
        var cts = _loadCts;
        _loadCts = null;
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void SetStatus(string message)
    {
        StatusMessage = message ?? string.Empty;
    }

    private static string ResolveDecisionConfirmation(string decision) => decision switch
    {
        ReviewDecisions.Approve => ReviewDisplay.ApprovedConfirmation,
        ReviewDecisions.Reject => ReviewDisplay.RejectedConfirmation,
        _ => ReviewDisplay.NeedsCorrectionConfirmation
    };

    private sealed class ActionObserver(ReviewViewModel owner) : IObserver<AppAction>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(AppAction value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                owner.Handle(value);
                return;
            }

            Dispatcher.UIThread.Post(() => owner.Handle(value));
        }
    }

    private void Handle(AppAction action)
    {
        if (_disposed)
        {
            return;
        }

        switch (action.Kind)
        {
            case AppActionKind.Highlight:
                HighlightField(action.Target);
                break;
            case AppActionKind.GotoPage when action.Page is { } page && page != CurrentPage:
                LoadPage(page);
                break;
            case AppActionKind.OpenDocument when Guid.TryParse(action.DocumentId, out var documentId):
                RequestDocument(documentId);
                break;
        }
    }
}

internal static class ReviewBrushKeys
{
    public const string Surface = "SurfaceBrush";
    public const string Surface2 = "Surface2Brush";
    public const string Primary = "PrimaryBrush";
    public const string Accent = "AccentBrush";
    public const string Success = "SuccessBrush";
    public const string Warning = "WarningBrush";
    public const string Danger = "DangerBrush";
    public const string Muted = "MutedForegroundBrush";
}

internal static class ReviewFieldStatuses
{
    public const string Detected = "detected";
    public const string LowConfidence = "low_confidence";
    public const string Missing = "missing";
    public const string UserConfirmed = "user_confirmed";
}

internal static class ReviewConfidenceTiers
{
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
    public const string Missing = "Missing";
}

internal static class ReviewReconciliationStatuses
{
    public const string Pass = "pass";
    public const string Fail = "fail";
    public const string Warning = "warning";
}

internal static class ReviewDecisions
{
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string NeedsCorrection = "NeedsCorrection";
    public const string Reviewer = "Reviewer";
}

internal static class ReviewDisplay
{
    public const string EmptyValue = "—";
    public const string NoDelta = "—";
    public const string LoadFailed = "Could not load the review payload.";
    public const string DocumentMissing = "This document is no longer available.";
    public const string SaveFailed = "Could not save the review. Try again.";
    public const string ApprovedConfirmation = "Document approved.";
    public const string RejectedConfirmation = "Document rejected.";
    public const string NeedsCorrectionConfirmation = "Sent back for correction.";
}

internal static class ReviewBrushPalette
{
    private const double CitationFillOpacity = 0.16d;
    private const double CitationActiveFillOpacity = 0.32d;

    private static readonly IBrush Fallback = new SolidColorBrush(Colors.Transparent);

    public static IBrush Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Fallback;
        }

        if (Application.Current is { } app
            && app.TryFindResource(key, app.ActualThemeVariant, out var value)
            && value is IBrush brush)
        {
            return brush;
        }

        return Fallback;
    }

    public static IBrush ResolveCitationFill(bool isActive)
    {
        var source = Resolve(isActive ? ReviewBrushKeys.Accent : ReviewBrushKeys.Primary);
        if (source is ISolidColorBrush solid)
        {
            var color = solid.Color;
            var opacity = isActive ? CitationActiveFillOpacity : CitationFillOpacity;
            return new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B));
        }

        return Fallback;
    }
}

internal static class ReviewImageResolver
{
    public static string? Resolve(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return FileOrNull(uri.LocalPath);
        }

        if (Path.IsPathFullyQualified(imageUrl))
        {
            return FileOrNull(imageUrl);
        }

        return null;
    }

    private static string? FileOrNull(string path) => File.Exists(path) ? path : null;
}
