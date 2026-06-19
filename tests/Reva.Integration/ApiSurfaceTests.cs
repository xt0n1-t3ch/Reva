using System.Net.Http.Json;
using System.Text.Json;

namespace Reva.Integration;

public sealed class ApiSurfaceTests(RevaWebApplicationFactory factory) : IClassFixture<RevaWebApplicationFactory>
{
    [Fact]
    public async Task OpenApiListsBackendRoutesAndSettingsSurfaces()
    {
        using var client = factory.CreateClient();

        var openApi = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/documents/{id}/review-payload", openApi);
        Assert.Contains("/api/settings", openApi);
        Assert.Contains("/api/inbound-sources", openApi);
        Assert.Contains("/api/agent-tools/get-review-payload/{id}", openApi);
    }

    [Fact]
    public async Task InboundSourcesReportDisabledOAuthAndSchemaIsValidJson()
    {
        using var client = factory.CreateClient();

        var sources = await client.GetStringAsync("/api/inbound-sources");
        var schema = await File.ReadAllTextAsync(TestPaths.ContractPath("bdx-review-payload.schema.json"));

        Assert.Contains("gmail", sources);
        Assert.Contains("requires OAuth credentials", sources);
        using var document = JsonDocument.Parse(schema);
        Assert.Equal("BdxReviewPayload", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AgentStatusReturnsRuntimeModelShape()
    {
        using var client = factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/agent/status"));
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("provider").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("model").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("baseUrl").GetString()));
        Assert.True(root.TryGetProperty("reachable", out _));
        Assert.True(root.TryGetProperty("modelAvailable", out _));
    }

    [Fact]
    public async Task SchemaMappingOverridePutPersistsSenderOverride()
    {
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/schema-mappings", new
        {
            senderKey = "broker.example",
            sourceHeader = "Loss Paid",
            canonicalField = "Claims",
            confidence = 0.87,
            isOverride = true
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("broker.example", root.GetProperty("senderKey").GetString());
        Assert.Equal("Loss Paid", root.GetProperty("sourceHeader").GetString());
        Assert.Equal("loss paid", root.GetProperty("normalizedSourceHeader").GetString());
        Assert.Equal("Claims", root.GetProperty("canonicalField").GetString());
        Assert.Equal(0.87, root.GetProperty("confidence").GetDouble(), 3);
        Assert.True(root.GetProperty("isOverride").GetBoolean());

        var mappings = await client.GetStringAsync("/api/schema-mappings");
        Assert.Contains("broker.example", mappings);
        Assert.Contains("Loss Paid", mappings);
    }

    [Fact]
    public async Task DataMaintenanceEndpointsClearAndReseedPersistWorkspaceState()
    {
        using var client = factory.CreateClient();

        var clearResponse = await client.PostAsync("/api/data/clear", null);
        clearResponse.EnsureSuccessStatusCode();
        using (var clearDocument = JsonDocument.Parse(await clearResponse.Content.ReadAsStringAsync()))
        {
            Assert.True(clearDocument.RootElement.TryGetProperty("deleted", out _));
        }

        var afterClear = await client.GetStringAsync("/api/documents");
        using (var clearList = JsonDocument.Parse(afterClear))
        {
            Assert.Equal(0, clearList.RootElement.GetArrayLength());
        }

        var reseedResponse = await client.PostAsync("/api/data/reseed", null);
        reseedResponse.EnsureSuccessStatusCode();
        using (var reseedDocument = JsonDocument.Parse(await reseedResponse.Content.ReadAsStringAsync()))
        {
            Assert.True(reseedDocument.RootElement.GetProperty("seeded").GetBoolean());
        }

        var afterReseed = await client.GetStringAsync("/api/documents");
        using var reseededList = JsonDocument.Parse(afterReseed);
        Assert.True(reseededList.RootElement.GetArrayLength() > 0);
    }

}
