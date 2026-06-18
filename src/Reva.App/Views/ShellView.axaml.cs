using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Reva.App.Onboarding;

namespace Reva.App.Views;

public partial class ShellView : UserControl
{
    private bool _autoStartEvaluated;

    public ShellView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_autoStartEvaluated)
        {
            return;
        }

        _autoStartEvaluated = true;
        ResolveTour()?.StartIfFirstRun();
    }

    private void OnStartTourClicked(object? sender, RoutedEventArgs e)
    {
        ResolveTour()?.Start();
    }

    private static ITourService? ResolveTour()
    {
        return TourServiceLocator.Resolve((Application.Current as App)?.Services);
    }
}
