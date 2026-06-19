using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Reva.App.Onboarding;

public partial class TourOverlayViewModel : ObservableObject
{
    private const double HighlightPadding = 8;
    private const double HighlightCornerRadius = 12;
    private const double CardWidth = 360;
    private const double CardGap = 16;
    private const double EdgeMargin = 24;
    private const double EstimatedCardHeight = 220;
    private const string NextLabel = "Next";
    private const string FinishLabel = "Finish";

    private readonly Func<bool> _canGoBack;
    private readonly Action _next;
    private readonly Action _back;
    private readonly Action _skip;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private string _counter = string.Empty;

    [ObservableProperty]
    private string _primaryActionText = NextLabel;

    [ObservableProperty]
    private bool _hasHighlight;

    [ObservableProperty]
    private double _stepTotal;

    [ObservableProperty]
    private double _stepProgress;

    [ObservableProperty]
    private bool _hasStepProgress;

    [ObservableProperty]
    private double _highlightX;

    [ObservableProperty]
    private double _highlightY;

    [ObservableProperty]
    private double _highlightWidth;

    [ObservableProperty]
    private double _highlightHeight;

    [ObservableProperty]
    private double _surfaceWidth;

    [ObservableProperty]
    private double _dimTopHeight;

    [ObservableProperty]
    private double _dimBottomY;

    [ObservableProperty]
    private double _dimBottomHeight;

    [ObservableProperty]
    private double _dimLeftWidth;

    [ObservableProperty]
    private double _dimRightX;

    [ObservableProperty]
    private double _dimRightWidth;

    [ObservableProperty]
    private double _dimMiddleY;

    [ObservableProperty]
    private double _dimMiddleHeight;

    [ObservableProperty]
    private Thickness _cardMargin;

    [ObservableProperty]
    private bool _cardCentered;

    public TourOverlayViewModel(Action next, Action back, Action skip, Func<bool> canGoBack)
    {
        _next = next;
        _back = back;
        _skip = skip;
        _canGoBack = canGoBack;
    }

    public bool CanGoBack => _canGoBack();

    [RelayCommand]
    private void Next() => _next();

    [RelayCommand]
    private void Back() => _back();

    [RelayCommand]
    private void Skip() => _skip();

    public void RaiseNavigationChanged() => OnPropertyChanged(nameof(CanGoBack));

    public void Present(
        TourStep step,
        int index,
        int total,
        Rect? targetBounds,
        Size surfaceSize)
    {
        Title = step.Title;
        Body = step.Body;
        Counter = total > 0 ? $"{index + 1} of {total}" : string.Empty;
        StepTotal = total;
        StepProgress = total > 0 ? index + 1 : 0;
        HasStepProgress = total > 0;
        PrimaryActionText = index >= total - 1 ? FinishLabel : NextLabel;
        RaiseNavigationChanged();

        if (surfaceSize.Width <= 0 || surfaceSize.Height <= 0)
        {
            ShowCentered();
            return;
        }

        var rect = targetBounds;
        if (rect is null || step.Placement == TourPlacement.Center)
        {
            ShowCentered();
            return;
        }

        var padded = Inflate(rect.Value, HighlightPadding, surfaceSize);
        if (padded.Width <= 0 || padded.Height <= 0)
        {
            ShowCentered();
            return;
        }

        ApplyHighlight(padded, surfaceSize);
        CardCentered = false;
        CardMargin = ResolveCardMargin(padded, step.Placement, surfaceSize);
        IsVisible = true;
    }

    private void ApplyHighlight(Rect padded, Size surfaceSize)
    {
        HasHighlight = true;
        SurfaceWidth = surfaceSize.Width;
        HighlightX = padded.X;
        HighlightY = padded.Y;
        HighlightWidth = padded.Width;
        HighlightHeight = padded.Height;

        DimTopHeight = padded.Y;
        DimBottomY = padded.Bottom;
        DimBottomHeight = Math.Max(0, surfaceSize.Height - padded.Bottom);
        DimMiddleY = padded.Y;
        DimMiddleHeight = padded.Height;
        DimLeftWidth = padded.X;
        DimRightX = padded.Right;
        DimRightWidth = Math.Max(0, surfaceSize.Width - padded.Right);
    }

    private void ShowCentered()
    {
        HasHighlight = false;
        CardCentered = true;
        CardMargin = default;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
        HasHighlight = false;
        CardCentered = false;
    }

    private static Rect Inflate(Rect rect, double amount, Size surfaceSize)
    {
        var left = Math.Max(0, rect.X - amount);
        var top = Math.Max(0, rect.Y - amount);
        var right = Math.Min(surfaceSize.Width, rect.Right + amount);
        var bottom = Math.Min(surfaceSize.Height, rect.Bottom + amount);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static Thickness ResolveCardMargin(Rect target, TourPlacement placement, Size surfaceSize)
    {
        var resolved = placement == TourPlacement.Auto
            ? ResolveAutoPlacement(target, surfaceSize)
            : placement;

        double left;
        double top;

        switch (resolved)
        {
            case TourPlacement.Above:
                left = ClampHorizontal(target.X, surfaceSize);
                top = Math.Max(EdgeMargin, target.Y - CardGap - EstimatedCardHeight);
                break;
            case TourPlacement.Left:
                left = Math.Max(EdgeMargin, target.X - CardGap - CardWidth);
                top = ClampVertical(target.Y, surfaceSize);
                break;
            case TourPlacement.Right:
                left = ClampHorizontal(target.Right + CardGap, surfaceSize);
                top = ClampVertical(target.Y, surfaceSize);
                break;
            default:
                left = ClampHorizontal(target.X, surfaceSize);
                top = Math.Min(surfaceSize.Height - EdgeMargin - EstimatedCardHeight, target.Bottom + CardGap);
                top = Math.Max(EdgeMargin, top);
                break;
        }

        return new Thickness(left, top, 0, 0);
    }

    private static TourPlacement ResolveAutoPlacement(Rect target, Size surfaceSize)
    {
        var spaceBelow = surfaceSize.Height - target.Bottom;
        var spaceAbove = target.Y;
        var spaceRight = surfaceSize.Width - target.Right;
        var spaceLeft = target.X;

        if (spaceBelow >= EstimatedCardHeight + CardGap + EdgeMargin)
        {
            return TourPlacement.Below;
        }

        if (spaceAbove >= EstimatedCardHeight + CardGap + EdgeMargin)
        {
            return TourPlacement.Above;
        }

        if (spaceRight >= CardWidth + CardGap + EdgeMargin)
        {
            return TourPlacement.Right;
        }

        if (spaceLeft >= CardWidth + CardGap + EdgeMargin)
        {
            return TourPlacement.Left;
        }

        return TourPlacement.Below;
    }

    private static double ClampHorizontal(double left, Size surfaceSize)
    {
        var maxLeft = surfaceSize.Width - EdgeMargin - CardWidth;
        return Math.Max(EdgeMargin, Math.Min(left, Math.Max(EdgeMargin, maxLeft)));
    }

    private static double ClampVertical(double top, Size surfaceSize)
    {
        var maxTop = surfaceSize.Height - EdgeMargin - EstimatedCardHeight;
        return Math.Max(EdgeMargin, Math.Min(top, Math.Max(EdgeMargin, maxTop)));
    }

    public double CardWidthValue { get; } = CardWidth;

    public CornerRadius HighlightCornerRadiusValue { get; } = new(HighlightCornerRadius);
}
