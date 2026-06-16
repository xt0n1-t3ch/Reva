using System.Diagnostics;
using System.Net.Sockets;

namespace Reva.E2E;

public sealed class RevaServerFixture : IAsyncLifetime
{
    private Process? _server;
    private string _tempRoot = string.Empty;

    public string BaseUrl { get; private set; } = string.Empty;
    public HttpClient Client { get; private set; } = default!;

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
            WorkingDirectory = Path.GetDirectoryName(dll)!
        };
        startInfo.ArgumentList.Add(dll);
        startInfo.ArgumentList.Add("--seed-demo");
        startInfo.ArgumentList.Add("--no-open");
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["Reva__Database__Provider"] = "Sqlite";
        startInfo.Environment["Reva__Database__ConnectionString"] = $"Data Source={Path.Combine(_tempRoot, "e2e.db")}";
        startInfo.Environment["Reva__Storage__UploadRoot"] = Path.Combine(_tempRoot, "uploads");

        _server = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the Reva server.");
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

        await WaitForHealthAsync(TimeSpan.FromSeconds(90));
    }

    private async Task WaitForHealthAsync(TimeSpan timeout)
    {
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
                var response = await Client.GetAsync("/health");
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

        throw new TimeoutException($"Reva server did not become healthy at {BaseUrl}.", last);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

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

    public Task DisposeAsync()
    {
        Client?.Dispose();

        try
        {
            if (_server is { HasExited: false })
            {
                _server.Kill(entireProcessTree: true);
                _server.WaitForExit(5000);
            }
        }
        finally
        {
            _server?.Dispose();
        }

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        return Task.CompletedTask;
    }
}

[CollectionDefinition("e2e")]
public sealed class E2ESuite : ICollectionFixture<RevaServerFixture>;
