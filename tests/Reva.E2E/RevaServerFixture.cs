using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace Reva.E2E;

// Launches the REAL application as a subprocess on a free port, against a throwaway database
// seeded with the demo corpus, then exposes a Playwright browser pointed at it. This is a true
// end-to-end harness: the same app a user runs, driven through a real browser.
public sealed class RevaServerFixture : IAsyncLifetime
{
    private Process? _server;
    private string _tempRoot = string.Empty;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = string.Empty;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var dll = LocateWebDll();
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _tempRoot = Path.Combine(Path.GetTempPath(), $"reva-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(dll)!
        };
        startInfo.ArgumentList.Add(dll);
        startInfo.ArgumentList.Add("--seed-demo");
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["Reva__Database__Provider"] = "Sqlite";
        startInfo.Environment["Reva__Database__ConnectionString"] = $"Data Source={Path.Combine(_tempRoot, "e2e.db")}";
        startInfo.Environment["Reva__Storage__UploadRoot"] = Path.Combine(_tempRoot, "uploads");

        _server = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the Reve server.");

        await WaitForHealthAsync(TimeSpan.FromSeconds(90));

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1440, Height = 960 } });
        return await context.NewPageAsync();
    }

    private async Task WaitForHealthAsync(TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            if (_server is { HasExited: true })
            {
                throw new InvalidOperationException($"Server exited early (code {_server.ExitCode}).");
            }

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                last = ex;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Reve server did not become healthy at {BaseUrl}.", last);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // The built web app DLL — prefer Release, fall back to Debug.
    private static string LocateWebDll()
    {
        var root = FindRepositoryRoot();
        string[] configs = ["Release", "Debug"];
        foreach (var config in configs)
        {
            var candidate = Path.Combine(root, "src", "Reva.Web", "bin", config, "net10.0", "Reva.dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Reva.Web has not been built. Run 'dotnet build' before the E2E tests.");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Reva.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing Reva.slnx was not found.");
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();

        try
        {
            if (_server is { HasExited: false })
            {
                _server.Kill(entireProcessTree: true);
                _server.WaitForExit(5000);
            }
        }
        catch
        {
            // Best-effort teardown.
        }
        finally
        {
            _server?.Dispose();
        }

        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Temp files are best-effort.
        }
    }
}

[CollectionDefinition("e2e")]
public sealed class E2ESuite : ICollectionFixture<RevaServerFixture>;
