using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Reva.Core.Contracts;
using Reva.Core.Settings;
using Reva.Infrastructure.Settings;

namespace Reva.Integration;

public sealed class SettingsTests(RevaWebApplicationFactory factory) : IClassFixture<RevaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task SettingsStoreReturnsDefaultsThenPersistsAndSanitizes()
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISettingsStore>();

        var initial = await store.GetAsync(CancellationToken.None);
        Assert.Equal(AppSettings.Default.ProductName, initial.ProductName);

        // Invalid accent is sanitized away; thresholds are clamped/ordered.
        var saved = await store.SaveAsync(
            new AppSettings(AppTheme.Dark, "not-a-hex", "Acme Reinsurance", 0.9, 0.4, null, 2, true), CancellationToken.None);
        Assert.Equal(string.Empty, saved.AccentColor);
        Assert.True(saved.ConfidenceLowMax <= saved.ConfidenceMediumMax);
        Assert.Equal(0.5, saved.ReconciliationTolerance);
        Assert.True(saved.UseLlmAssist);

        var validAccent = await store.SaveAsync(
            saved with { AccentColor = "#4F46E5" }, CancellationToken.None);
        Assert.Equal("#4f46e5", validAccent.AccentColor);

        // Reload from a fresh scope: it persisted.
        using var scope2 = factory.Services.CreateScope();
        var reloaded = await scope2.ServiceProvider.GetRequiredService<ISettingsStore>().GetAsync(CancellationToken.None);
        Assert.Equal("Acme Reinsurance", reloaded.ProductName);
        Assert.Equal(AppTheme.Dark, reloaded.Theme);

        await store.SaveAsync(AppSettings.Default, CancellationToken.None);
    }

    [Fact]
    public async Task SettingsApiReturnsDefaultsThenPersistsAndSanitizes()
    {
        using var client = factory.CreateClient();

        var initial = await client.GetFromJsonAsync<AppSettings>("/api/settings", SerializerOptions);
        Assert.NotNull(initial);
        Assert.Equal(AppSettings.Default.ProductName, initial.ProductName);

        var response = await client.PutAsJsonAsync(
            "/api/settings",
            new AppSettings(AppTheme.Dark, "not-a-hex", "Acme Reinsurance API", 0.95, 0.2, null, -1, true),
            SerializerOptions);
        response.EnsureSuccessStatusCode();

        var saved = await response.Content.ReadFromJsonAsync<AppSettings>(SerializerOptions);
        Assert.NotNull(saved);
        Assert.Equal(string.Empty, saved.AccentColor);
        Assert.Equal("Acme Reinsurance API", saved.ProductName);
        Assert.True(saved.ConfidenceLowMax <= saved.ConfidenceMediumMax);
        Assert.Equal(0, saved.ReconciliationTolerance);
        Assert.True(saved.UseLlmAssist);

        await client.PutAsJsonAsync("/api/settings", AppSettings.Default, SerializerOptions);
    }

    [Fact]
    public async Task ClearAllDocumentsEmptiesTheWorkspace()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(File.ReadAllBytes(SamplePath("bordereau.csv"))), "file", "bordereau.csv");
        await client.PostAsync("/api/documents/", multipart);

        using var scope = factory.Services.CreateScope();
        var maintenance = scope.ServiceProvider.GetRequiredService<IDataMaintenance>();
        var removed = await maintenance.ClearAllDocumentsAsync(CancellationToken.None);
        Assert.True(removed >= 1);

        var documents = await client.GetFromJsonAsync<List<DocumentSummary>>("/api/documents/", SerializerOptions);
        Assert.NotNull(documents);
        Assert.Empty(documents);
    }

    private static string SamplePath(string name)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", name);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Sample file was not found: {name}.");
    }
}
