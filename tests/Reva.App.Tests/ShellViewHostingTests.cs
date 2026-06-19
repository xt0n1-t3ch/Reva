using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reva.App.Navigation;
using Reva.App.ViewModels;
using Reva.App.Views;

namespace Reva.App.Tests;

public sealed class ShellViewHostingTests
{
    [AvaloniaFact]
    public void ShellViewHostsShellViewModelAndShowsDashboardByDefault()
    {
        using var harness = ShellHarness.Create();
        var window = ShowShell(harness.Shell);
        try
        {
            Assert.IsType<DashboardViewModel>(harness.Shell.Current);
            Assert.Equal(AppRoutes.Dashboard, harness.Shell.ActiveRoute);
            Assert.IsType<DashboardView>(HostedView(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NavigateSwitchesCurrentAcrossEveryRoute()
    {
        using var harness = ShellHarness.Create();
        var window = ShowShell(harness.Shell);
        try
        {
            AssertNavigates(harness, window, AppRoutes.Review, typeof(ReviewViewModel), typeof(ReviewView));
            AssertNavigates(harness, window, AppRoutes.Mappings, typeof(MappingsViewModel), typeof(MappingsView));
            AssertNavigates(harness, window, AppRoutes.Export, typeof(ExportViewModel), typeof(ExportView));
            AssertNavigates(harness, window, AppRoutes.Settings, typeof(SettingsViewModel), typeof(SettingsView));
            AssertNavigates(harness, window, AppRoutes.Dashboard, typeof(DashboardViewModel), typeof(DashboardView));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NavigateCommandFromShellUpdatesHostedContent()
    {
        using var harness = ShellHarness.Create();
        var window = ShowShell(harness.Shell);
        try
        {
            harness.Shell.NavigateCommand.Execute(AppRoutes.Settings);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.IsType<SettingsViewModel>(harness.Shell.Current);
            Assert.Equal(AppRoutes.Settings, harness.Shell.ActiveRoute);
            Assert.IsType<SettingsView>(HostedView(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NavigatingToSameRouteKeepsSingleCurrentInstance()
    {
        using var harness = ShellHarness.Create();
        var window = ShowShell(harness.Shell);
        try
        {
            harness.Shell.NavigateCommand.Execute(AppRoutes.Review);
            Dispatcher.UIThread.RunJobs();
            var first = harness.Shell.Current;

            harness.Shell.NavigateCommand.Execute(AppRoutes.Review);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(first, harness.Shell.Current);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertNavigates(
        ShellHarness harness,
        Window window,
        string route,
        Type expectedViewModel,
        Type expectedView)
    {
        harness.Navigation.NavigateTo(route);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(expectedViewModel, harness.Shell.Current?.GetType());
        Assert.Equal(route, harness.Shell.ActiveRoute);
        Assert.Equal(expectedView, HostedView(window)?.GetType());
    }

    private static Window ShowShell(ShellViewModel shell)
    {
        var window = new Window
        {
            Width = 1280,
            Height = 800,
            Content = new ShellView { DataContext = shell }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static Control? HostedView(Window window)
    {
        var host = window.GetVisualDescendants()
            .OfType<ContentControl>()
            .FirstOrDefault(control => control.Name == "TourTargetContentHost");
        if (host is null)
        {
            return null;
        }

        if (host.Presenter?.Child is Control realized)
        {
            return realized;
        }

        return host.GetVisualDescendants()
            .OfType<UserControl>()
            .FirstOrDefault();
    }
}
