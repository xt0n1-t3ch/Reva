using Avalonia.Controls;
using Avalonia.Interactivity;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class SettingsView : UserControl
{
    private bool _initialized;

    public SettingsView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_initialized || DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        _initialized = true;
        await viewModel.InitializeAsync();
    }
}
