using Microsoft.Extensions.Options;
using Reva.Infrastructure.Ingestion;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class IngestionSeamTests
{
    [Fact]
    public void DoclingAdapterRespectsFeatureFlag()
    {
        var parser = new ParserRouter();
        Assert.False(new OptionalDoclingDocumentParser(Options.Create(new DoclingFeatureOptions()), parser).CanParse("sample.pdf"));
        Assert.True(new OptionalDoclingDocumentParser(Options.Create(new DoclingFeatureOptions { Enabled = true }), parser).CanParse("sample.pdf"));
    }

    [Fact]
    public async Task FileEmailSourcePullsEmlFilesOnly()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"reva-inbound-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "sample.eml"), "Subject: Test");
        await File.WriteAllTextAsync(Path.Combine(directory, "ignore.txt"), "ignored");

        var source = new FileEmailInboundDocumentSource(Options.Create(new FileEmailInboundOptions { Directory = directory }));
        var documents = await source.PullAsync(CancellationToken.None);

        Assert.Single(documents);
        Assert.Equal("sample.eml", documents[0].FileName);
        documents[0].Content.Dispose();
        Directory.Delete(directory, true);
    }
}
