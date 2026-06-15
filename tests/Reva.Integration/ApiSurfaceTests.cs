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
        var schema = await File.ReadAllTextAsync(FindSchemaPath());

        Assert.Contains("gmail", sources);
        Assert.Contains("requires OAuth credentials", sources);
        using var document = JsonDocument.Parse(schema);
        Assert.Equal("BdxReviewPayload", document.RootElement.GetProperty("title").GetString());
    }

    private static string FindSchemaPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "contracts", "bdx-review-payload.schema.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("bdx-review-payload.schema.json was not found.");
    }
}
