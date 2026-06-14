using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Reva.Core.Contracts;
using Reva.Core.Settings;
using Reva.Infrastructure.Settings;
using Reva.Web.Components.Services;

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
            new AppSettings(AppTheme.Dark, "not-a-hex", "Acme Reinsurance", 0.9, 0.4, null), CancellationToken.None);
        Assert.Equal(string.Empty, saved.AccentColor);
        Assert.True(saved.ConfidenceLowMax <= saved.ConfidenceMediumMax);

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
    public void ConfidenceTiersFollowRuntimeSettings()
    {
        try
        {
            RuntimeSettings.Set(AppSettings.Default with { ConfidenceLowMax = 0.5, ConfidenceMediumMax = 0.8 });
            Assert.Equal("Low", CockpitFormat.AgreementLabel(0.4));
            Assert.Equal("Medium", CockpitFormat.AgreementLabel(0.7));
            Assert.Equal("High", CockpitFormat.AgreementLabel(0.95));
            Assert.Equal("lvl-high", CockpitFormat.ConfidenceLevel(0.9));
        }
        finally
        {
            RuntimeSettings.Set(AppSettings.Default);
        }
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
