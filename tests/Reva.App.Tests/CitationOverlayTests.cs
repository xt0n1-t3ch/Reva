using Avalonia.Headless.XUnit;
using Reva.App.ViewModels;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Agent;

namespace Reva.App.Tests;

public sealed class CitationOverlayTests
{
    [AvaloniaFact]
    public void CitationRegionProjectScalesNormalizedRectToSurface()
    {
        var region = new CitationRegion("premium", 1, new SourceBox(0.25, 0.5, 0.2, 0.1), "quote");

        region.Project(1000d, 800d);

        Assert.Equal(250d, region.Left, 3);
        Assert.Equal(400d, region.Top, 3);
        Assert.Equal(200d, region.Width, 3);
        Assert.Equal(80d, region.Height, 3);
    }

    [AvaloniaFact]
    public void CitationRegionClampsOutOfRangeNormalizedValues()
    {
        var region = new CitationRegion("k", 1, new SourceBox(-0.5, 1.4, 2.0, double.NaN), null);

        Assert.Equal(0d, region.NormalizedX, 6);
        Assert.Equal(1d, region.NormalizedY, 6);
        Assert.Equal(1d, region.NormalizedWidth, 6);
        Assert.Equal(0d, region.NormalizedHeight, 6);

        region.Project(500d, 500d);

        Assert.Equal(0d, region.Left, 3);
        Assert.Equal(500d, region.Top, 3);
        Assert.Equal(500d, region.Width, 3);
        Assert.Equal(0d, region.Height, 3);
    }

    [AvaloniaFact]
    public async Task ReviewViewModelProjectsCitationsWhenSurfaceUpdated()
    {
        var documentId = Guid.NewGuid();
        var payload = BuildPayloadWithCitation(documentId, new SourceBox(0.1, 0.2, 0.3, 0.4));
        var summary = new DocumentSummary(
            documentId,
            "doc.pdf",
            DocumentStatus.Extracted,
            ReviewState.Pending,
            ReinsuranceDocumentType.FacultativeSlip,
            0.9,
            0,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        var client = new FakeRevaClient
        {
            Documents = [summary],
            ReviewPayload = payload,
            ReviewDocumentId = documentId
        };
        var bus = new AppActionBus();
        using var vm = new ReviewViewModel(client, bus);

        await vm.InitializeAsync(CancellationToken.None);
        vm.UpdateSurface(800d, 600d);

        Assert.True(vm.HasCitations);
        var citation = Assert.Single(vm.Citations);
        Assert.Equal(80d, citation.Left, 3);
        Assert.Equal(120d, citation.Top, 3);
        Assert.Equal(240d, citation.Width, 3);
        Assert.Equal(240d, citation.Height, 3);
    }

    private static BdxReviewPayload BuildPayloadWithCitation(Guid documentId, SourceBox box)
    {
        var citation = new Citation("span-1", 1, box, "quote", "primary");
        var provenance = new FieldProvenance("deterministic", "step-1", null, null, [citation]);
        var field = new FieldValue("premium", "Premium", "100", null, "detected", 0.9, provenance);
        var document = new BdxDocument(documentId, "doc.pdf", [new BdxPage(1, string.Empty, 800d, 600d, 0)]);
        return new BdxReviewPayload(document, [], [field], [], []);
    }
}
