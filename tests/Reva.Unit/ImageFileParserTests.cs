using Reva.Core.Contracts;
using Reva.Infrastructure.Ocr;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class ImageFileParserTests
{
    [Fact]
    public async Task ParseAsyncUsesOcrDimensionsForPageAndSpans()
    {
        var path = TempPath(".png");
        var bbox = new SourceBox(0.1, 0.2, 0.3, 0.04);
        try
        {
            await File.WriteAllBytesAsync(path, [0, 1, 2]);
            var parser = new ImageFileParser(new FakeOcrEngine(new OcrResult(
                "Cedent Halcyon Mutual Insurance",
                [new OcrLine("Cedent Halcyon Mutual Insurance", 0.95, 1, bbox, null)],
                0.95,
                1200,
                1600)));

            var parsed = await parser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("image-ocr", parsed.ParserProfile);
            var page = Assert.Single(parsed.Pages);
            Assert.Equal(1, page.Page);
            Assert.Equal(path, page.ImagePath);
            Assert.Equal(1200d, page.Width);
            Assert.Equal(1600d, page.Height);
            var span = Assert.Single(parsed.SourceSpans);
            Assert.Equal(1200d, span.PageWidth);
            Assert.Equal(1600d, span.PageHeight);
            Assert.Equal(bbox, span.Bbox);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncDoesNotEmitPageWhenOcrHasNoDimensions()
    {
        var path = TempPath(".png");
        try
        {
            await File.WriteAllBytesAsync(path, [0, 1, 2]);
            var parser = new ImageFileParser(new FakeOcrEngine(OcrResult.Empty));

            var parsed = await parser.ParseAsync(path, CancellationToken.None);

            Assert.Empty(parsed.Pages);
            Assert.NotEmpty(parsed.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"reva-test-{Guid.NewGuid():N}{extension}");

    private sealed class FakeOcrEngine(OcrResult result) : IOcrEngine
    {
        public OcrResult Recognize(string imagePath, CancellationToken cancellationToken) => result;
    }
}
