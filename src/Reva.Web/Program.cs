using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Reva.Infrastructure;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Persistence.DemoData;
using Reva.Web.Components;
using Reva.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
// Whether the end-user escape hatches permit auto-opening a browser. The actual open is
// further restricted to Production below, so running locally or under tests never spawns a tab.
var allowBrowserOpen = !args.Contains("--no-open", StringComparer.OrdinalIgnoreCase)
    && !string.Equals(Environment.GetEnvironmentVariable("REVA_NO_OPEN"), "1", StringComparison.OrdinalIgnoreCase);
var seedDemo = args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("REVA_SEED_DEMO"), "1", StringComparison.OrdinalIgnoreCase);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRevaInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
    await dbContext.Database.MigrateAsync();

    // Load persisted settings into the runtime holder so the UI reflects them from the first render.
    await scope.ServiceProvider.GetRequiredService<Reva.Infrastructure.Settings.ISettingsStore>().GetAsync(CancellationToken.None);

    if (seedDemo)
    {
        var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
        await DemoDocumentSeeder.SeedIfEmptyAsync(workflow, dbContext, CancellationToken.None);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Re-execute to the friendly not-found page for UI navigation only. API routes must keep their
// raw status codes (otherwise a JSON 404 gets re-run against the Razor page and surfaces as 405).
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Reva" }));
app.MapDocumentEndpoints();
app.MapTemplateEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.StartAsync();

// Only the packaged, end-user (Production) launch opens a browser. Local `dotnet run`,
// integration tests, and the e2e harness all run in Development/Testing and stay quiet.
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

public partial class Program;
