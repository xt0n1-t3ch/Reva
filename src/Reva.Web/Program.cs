using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Reva.Infrastructure;
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

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Reva" }));
app.MapOpenApi();
app.MapDocumentEndpoints();
app.MapTemplateEndpoints();
app.MapApiSurfaceEndpoints();

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

public partial class Program;
