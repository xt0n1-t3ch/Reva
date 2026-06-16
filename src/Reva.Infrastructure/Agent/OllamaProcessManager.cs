using System.Diagnostics;

namespace Reva.Infrastructure.Agent;

// Best-effort local model lifecycle for the packaged desktop app: if the agent's Ollama
// endpoint is unreachable and the `ollama` CLI is installed, start `ollama serve` in the
// background. Never throws and never blocks startup — the chat degrades gracefully when no
// model is available, and every other feature is fully deterministic without it.
public static class OllamaProcessManager
{
    private static readonly HttpClient Probe = new() { Timeout = TimeSpan.FromSeconds(2) };

    public static Uri RootFrom(string baseUrl)
    {
        var uri = new Uri(baseUrl, UriKind.Absolute);
        return new Uri(uri.GetLeftPart(UriPartial.Authority));
    }

    public static async Task<bool> IsRunningAsync(string baseUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Probe.GetAsync(new Uri(RootFrom(baseUrl), "api/tags"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> HasModelAsync(string baseUrl, string model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Probe.GetAsync(new Uri(RootFrom(baseUrl), "api/tags"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Contains(model, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static async Task TryEnsureRunningAsync(string baseUrl, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || await IsRunningAsync(baseUrl, cancellationToken))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("ollama", "serve")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
        }
        catch
        {
            // ollama is not installed or not on PATH; the chat will report it is unavailable.
        }
    }
}
