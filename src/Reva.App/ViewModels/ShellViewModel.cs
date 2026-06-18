using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.App.Navigation;
using Reva.App.Services;

namespace Reva.App.ViewModels;

public partial class ShellViewModel : ViewModelBase, IDisposable
{
    private const string OfflineStatus = "Offline";
    private const string OnlineStatus = "Online";

    private readonly INavigationService _navigation;
    private readonly IRevaClient _client;

    [ObservableProperty]
    private ViewModelBase? _current;

    [ObservableProperty]
    private string _activeRoute = AppRoutes.Dashboard;

    [ObservableProperty]
    private string _activeModel = string.Empty;

    [ObservableProperty]
    private string _statusText = OfflineStatus;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _isCopilotOpen;

    public ShellViewModel(INavigationService navigation, IRevaClient client, CopilotViewModel copilot)
    {
        _navigation = navigation;
        _client = client;
        Copilot = copilot;
        _navigation.CurrentChanged += OnNavigationCurrentChanged;
        _navigation.NavigateTo(AppRoutes.Dashboard);
    }

    public CopilotViewModel Copilot { get; }

#pragma warning disable CA1822
    public string AppTitle => "Reva";
#pragma warning restore CA1822

    [RelayCommand]
    private void Navigate(string route) => _navigation.NavigateTo(route);

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    [RelayCommand]
    private void ToggleCopilot() => IsCopilotOpen = !IsCopilotOpen;

#pragma warning disable CA1822
    [RelayCommand]
    private void StartTour()
    {
    }
#pragma warning restore CA1822

    public async Task RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var online = await _client.IsOllamaAvailableAsync(cancellationToken);
            var model = await _client.GetActiveModelAsync(cancellationToken);
            IsOnline = online;
            StatusText = online ? OnlineStatus : OfflineStatus;
            ActiveModel = string.IsNullOrWhiteSpace(model) ? string.Empty : model!;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            IsOnline = false;
            StatusText = OfflineStatus;
        }
    }

    private void OnNavigationCurrentChanged(ViewModelBase viewModel)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyCurrent(viewModel);
            return;
        }

        Dispatcher.UIThread.Post(() => ApplyCurrent(viewModel));
    }

    private void ApplyCurrent(ViewModelBase viewModel)
    {
        Current = viewModel;
        ActiveRoute = _navigation.CurrentRoute ?? AppRoutes.Dashboard;
    }

    public void Dispose()
    {
        _navigation.CurrentChanged -= OnNavigationCurrentChanged;
        GC.SuppressFinalize(this);
    }
}
