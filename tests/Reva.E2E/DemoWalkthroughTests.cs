using System.Text.Json;
using Microsoft.Playwright;

namespace Reva.E2E;

// The interview demo, codified. Each test drives the real browser through a step of the
// walkthrough and asserts the behaviour we would show on screen.
[Collection("e2e")]
public sealed class DemoWalkthroughTests(RevaServerFixture server)
{
    [Fact]
    public async Task WorkspaceLoadsAndQueueFilterFilters()
    {
        var page = await server.NewPageAsync();
        await page.GotoAsync(server.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Work queue" })).ToBeVisibleAsync();

        var allRows = await page.Locator(".queue-table tbody tr").CountAsync();
        Assert.True(allRows >= 3, $"Expected the seeded corpus in the queue, saw {allRows} rows.");

        // The "Clean" filter must actually narrow the queue (only documents with zero checks).
        await page.GetByRole(AriaRole.Button, new() { Name = "Clean" }).ClickAsync();
        await Assertions.Expect(page.Locator(".queue-table tbody tr")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ReviewShowsSourceCitationAndOffersExports()
    {
        var page = await server.NewPageAsync();

        // Navigate straight to the hero bordereau's review (fresh interactive circuit).
        using var http = new HttpClient();
        var listJson = await http.GetStringAsync($"{server.BaseUrl}/api/documents/");
        using var docs = JsonDocument.Parse(listJson);
        // Pick the hero bordereau specifically (not whatever happens to be newest — other tests
        // upload files and would otherwise shift index 0).
        var heroId = docs.RootElement.EnumerateArray()
            .First(d => (d.GetProperty("fileName").GetString() ?? string.Empty).Contains("orion", StringComparison.OrdinalIgnoreCase))
            .GetProperty("id").GetString();

        await page.GotoAsync($"{server.BaseUrl}/review/{heroId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Extracted fields" })).ToBeVisibleAsync();

        // Hovering the Broker field highlights its value in the document — the source citation.
        // (Dispatched as a bubbling event so Blazor's root-level delegated handler receives it,
        // then we wait for the server round-trip that injects the highlight.)
        // Hovering the Broker field highlights its value in the document — the source citation.
        // Re-dispatch the bubbling hover each tick until the interactive circuit (which connects
        // a moment after navigation) processes it and the server injects the highlight.
        var cited = await page.EvaluateAsync<string?>(@"async () => {
            for (let i = 0; i < 60; i++) {
                const row = [...document.querySelectorAll('.field-edit-row')].find(x => x.querySelector('.field-edit-label')?.textContent.trim() === 'Broker');
                if (row) { row.dispatchEvent(new MouseEvent('mouseover', { bubbles: true })); }
                await new Promise(r => setTimeout(r, 250));
                const mark = document.querySelector('mark.cite');
                if (mark) return mark.textContent;
            }
            return null;
        }");
        Assert.Equal("Global Re Solutions", cited);

        // The reconciliation checks render with detected-vs-expected values.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Checks" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Detected").First).ToBeVisibleAsync();

        // Exports are offered and point at the real API.
        var json = page.GetByRole(AriaRole.Link, new() { Name = "Export as JSON" });
        await Assertions.Expect(json).ToBeVisibleAsync();
        var href = await json.GetAttributeAsync("href");
        Assert.Contains("export?format=json", href);
    }

    [Fact]
    public async Task ImportingUnrecognizedFileIsNeverRejected()
    {
        var page = await server.NewPageAsync();
        await page.GotoAsync(server.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Drop a random binary file: it must still become a reviewable record (best-effort, low confidence).
        await page.Locator("input[type=file]").First.SetInputFilesAsync(new FilePayload
        {
            Name = "mystery.bin",
            MimeType = "application/octet-stream",
            Buffer = "%PDF-1.4 random ANTONIO RESUME bytes  "u8.ToArray()
        });

        // Upload navigates straight into the review of the new document.
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("/review/"), new() { Timeout = 30000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review decision" })).ToBeVisibleAsync();
    }
}
