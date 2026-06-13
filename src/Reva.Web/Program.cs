using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Reva.Infrastructure;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Persistence.DemoData;
using Reva.Web.Components;
using Reva.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var launchBrowser = !args.Contains("--no-open", StringComparer.OrdinalIgnoreCase)
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Reva" }));
app.MapDocumentEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.StartAsync();

if (launchBrowser)
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
