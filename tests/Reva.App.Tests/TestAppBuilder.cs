using Avalonia;
using Avalonia.Headless;
using Reva.App;
using Reva.App.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Reva.App.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            });
}
