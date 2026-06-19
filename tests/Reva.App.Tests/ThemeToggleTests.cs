using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace Reva.App.Tests;

public sealed class ThemeToggleTests
{
    [AvaloniaFact]
    public void ToggleThemeFlipsApplicationRequestedThemeVariantLightToDark()
    {
        var application = Application.Current!;
        application.RequestedThemeVariant = ThemeVariant.Light;

        using var harness = ShellHarness.Create();

        harness.Shell.ToggleThemeCommand.Execute(null);

        Assert.True(harness.Shell.IsDarkTheme);
        Assert.Equal(ThemeVariant.Dark, application.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void ToggleThemeTwiceReturnsToLight()
    {
        var application = Application.Current!;
        application.RequestedThemeVariant = ThemeVariant.Light;

        using var harness = ShellHarness.Create();

        harness.Shell.ToggleThemeCommand.Execute(null);
        harness.Shell.ToggleThemeCommand.Execute(null);

        Assert.False(harness.Shell.IsDarkTheme);
        Assert.Equal(ThemeVariant.Light, application.RequestedThemeVariant);
    }
}
