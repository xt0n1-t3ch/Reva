using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class ReviewView : UserControl
{
    private bool _initialized;

    public ReviewView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_initialized || DataContext is not ReviewViewModel viewModel)
        {
            return;
        }

        _initialized = true;
        await viewModel.InitializeAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (DataContext is ReviewViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }

    private void OnOverlaySizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ReviewViewModel viewModel)
        {
            viewModel.UpdateSurface(e.NewSize.Width, e.NewSize.Height);
        }
    }

    private void OnFieldPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: ReviewFieldItem field })
        {
            field.HoverEnterCommand.Execute(null);
        }
    }

    private void OnFieldPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: ReviewFieldItem field })
        {
            field.HoverLeaveCommand.Execute(null);
        }
    }
}
