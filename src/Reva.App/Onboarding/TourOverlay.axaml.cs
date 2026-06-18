using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Reva.App.Onboarding;

public partial class TourOverlay : UserControl
{
    private readonly TourOverlayViewModel _viewModel;
    private ITourService? _service;
    private Visual? _surface;

    public TourOverlay()
    {
        _viewModel = new TourOverlayViewModel(OnNext, OnBack, OnSkip, CanGoBack);
        DataContext = _viewModel;
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _surface = ResolveSurface();
        _service = ResolveService();

        if (_service is not null)
        {
            _service.StepChanged += OnStepChanged;
            _service.Stopped += OnStopped;

            if (_service.IsRunning)
            {
                ScheduleRender();
            }
        }

        if (_surface is not null)
        {
            _surface.PropertyChanged += OnSurfacePropertyChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_service is not null)
        {
            _service.StepChanged -= OnStepChanged;
            _service.Stopped -= OnStopped;
            _service = null;
        }

        if (_surface is not null)
        {
            _surface.PropertyChanged -= OnSurfacePropertyChanged;
            _surface = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private Visual? ResolveSurface()
    {
        return this.FindAncestorOfType<Window>() as Visual ?? (TopLevel.GetTopLevel(this) as Visual);
    }

    private static ITourService? ResolveService()
    {
        return TourServiceLocator.Resolve((Application.Current as App)?.Services);
    }

    private bool CanGoBack() => _service is { IsRunning: true } service && service.CurrentIndex > 0;

    private void OnNext() => _service?.Advance();

    private void OnBack() => _service?.Back();

    private void OnSkip() => _service?.Skip();

    private void OnStepChanged() => ScheduleRender();

    private void OnStopped()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _viewModel.Hide();
            return;
        }

        Dispatcher.UIThread.Post(_viewModel.Hide);
    }

    private void OnSurfacePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && _service is { IsRunning: true })
        {
            ScheduleRender();
        }
    }

    private void ScheduleRender()
    {
        Dispatcher.UIThread.Post(() => Render(allowRetry: true), DispatcherPriority.Loaded);
    }

    private void Render(bool allowRetry)
    {
        if (_service is not { IsRunning: true } service)
        {
            _viewModel.Hide();
            return;
        }

        var step = service.CurrentStep;
        if (step is null)
        {
            _viewModel.Hide();
            return;
        }

        var bounds = TourTargetResolver.ResolveBounds(_surface, this, step.TargetName);
        _viewModel.Present(step, service.CurrentIndex, service.StepCount, bounds, Bounds.Size);

        if (bounds is null && allowRetry && !string.IsNullOrWhiteSpace(step.TargetName))
        {
            Dispatcher.UIThread.Post(() => Render(allowRetry: false), DispatcherPriority.Background);
        }
    }
}
