using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Reva.E2E;

[Collection("e2e")]
public sealed class ApiSmokeTests(RevaServerFixture server)
{
    [Fact]
    public async Task ApiHostServesHealthDocumentsAndReviewPayload()
    {
        var health = await server.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        using var documentsResponse = await server.Client.GetAsync("/api/documents");
        Assert.Equal(HttpStatusCode.OK, documentsResponse.StatusCode);

        await using var documentsStream = await documentsResponse.Content.ReadAsStreamAsync();
        using var documents = await JsonDocument.ParseAsync(documentsStream);
        Assert.Equal(JsonValueKind.Array, documents.RootElement.ValueKind);
        var firstDocument = documents.RootElement.EnumerateArray().FirstOrDefault();
        Assert.NotEqual(JsonValueKind.Undefined, firstDocument.ValueKind);
        var documentId = firstDocument.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(documentId));

        using var payloadResponse = await server.Client.GetAsync($"/api/documents/{documentId}/review-payload");
        Assert.Equal(HttpStatusCode.OK, payloadResponse.StatusCode);

        var payload = await payloadResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("document", out var document));
        Assert.Equal(documentId, document.GetProperty("id").GetString());
        Assert.True(payload.TryGetProperty("fields", out var fields));
        Assert.Equal(JsonValueKind.Array, fields.ValueKind);
        Assert.True(payload.TryGetProperty("sourceSpans", out var sourceSpans));
        Assert.Equal(JsonValueKind.Array, sourceSpans.ValueKind);
        Assert.True(payload.TryGetProperty("reconciliation", out var reconciliation));
        Assert.Equal(JsonValueKind.Array, reconciliation.ValueKind);
    }
}
