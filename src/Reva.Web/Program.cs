using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Persistence.DemoData;
using Reva.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var allowBrowserOpen = !args.Contains("--no-open", StringComparer.OrdinalIgnoreCase)
    && !string.Equals(Environment.GetEnvironmentVariable("REVA_NO_OPEN"), "1", StringComparison.OrdinalIgnoreCase);
var seedDemo = args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("REVA_SEED_DEMO"), "1", StringComparison.OrdinalIgnoreCase);

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var frontendOrigin = builder.Configuration["Reva:Frontend:Origin"] ?? "http://localhost:3000";
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddRevaInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
    await dbContext.Database.MigrateAsync();

    await scope.ServiceProvider.GetRequiredService<Reva.Infrastructure.Settings.ISettingsStore>().GetAsync(CancellationToken.None);

    if (seedDemo)
    {
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        await DemoDocumentSeeder.SeedIfEmptyAsync(workflow, dbContext, CancellationToken.None);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }));
}

app.UseCors(FrontendCorsPolicy);

// Serve the packaged single-page UI (the static export staged into wwwroot at package time).
// In dev wwwroot is empty and the Next.js dev server hosts the UI instead.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Reva" }));
app.MapGet("/api/agent/status", async (IOptions<AgentChatOptions> options, CancellationToken cancellationToken) =>
{
    var baseUrl = options.Value.BaseUrl;
    var running = await OllamaProcessManager.IsRunningAsync(baseUrl, cancellationToken);
    var modelAvailable = running && await OllamaProcessManager.HasModelAsync(baseUrl, options.Value.Model, cancellationToken);
    return Results.Ok(new { ollamaRunning = running, modelAvailable, model = options.Value.Model });
}).WithTags("Agent");
app.MapOpenApi();
app.MapDocumentEndpoints();
app.MapTemplateEndpoints();
app.MapApiSurfaceEndpoints();
app.MapAgentEndpoints();

// SPA fallback: client routes (e.g. /review) resolve to their static HTML, otherwise to index.html.
app.MapFallback(SpaFallback);

// Best-effort: start a local Ollama if one is installed but not yet running. Never blocks startup.
_ = OllamaProcessManager.TryEnsureRunningAsync(
    app.Configuration[RevaConfigurationKeys.AgentBaseUrl] ?? AgentChatOptions.DefaultBaseUrl,
    CancellationToken.None);

await app.StartAsync();

if (allowBrowserOpen && app.Environment.IsProduction())
{
    OpenBrowser(app);
}

await app.WaitForShutdownAsync();

static void OpenBrowser(WebApplication app)
{
    var url = app.Urls.FirstOrDefault(value => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        ?? app.Urls.FirstOrDefault()
        ?? "http://localhost:5187";
    url = url.Replace("http://0.0.0.0", "http://localhost", StringComparison.OrdinalIgnoreCase)
        .Replace("http://[::]", "http://localhost", StringComparison.OrdinalIgnoreCase);
    Process.Start(new ProcessStartInfo(url)
    {
        UseShellExecute = true
    });
}

static async Task SpaFallback(HttpContext context)
{
    var webRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
    if (string.IsNullOrEmpty(webRoot))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var relative = context.Request.Path.Value?.Trim('/') ?? string.Empty;
    var candidate = string.IsNullOrEmpty(relative) ? "index.html" : relative + ".html";
    var file = ResolveWithinWebRoot(webRoot, candidate) ?? ResolveWithinWebRoot(webRoot, "index.html");
    if (file is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(file);

    static string? ResolveWithinWebRoot(string webRoot, string candidate)
    {
        var root = Path.GetFullPath(webRoot);
        var full = Path.GetFullPath(Path.Combine(root, candidate));
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full) ? full : null;
    }
}

public partial class Program;
