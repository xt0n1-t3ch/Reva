using System.Text;
using Reva.Core.Contracts;
using Reva.Infrastructure.Ocr;
using Reva.Infrastructure.Rendering;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Reva.Infrastructure.Parsing;

public sealed class PdfFileParser(IOcrEngine? ocr = null, IPdfPageImageRenderer? renderer = null) : IFileParser
{
    public const int SparseTextThreshold = 24;
    public const int MaxOcrPages = 8;

    public string Profile => "pdf-text";

    public bool CanParse(string extension) => extension == ".pdf";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var pageCount = 0;
        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                pageCount++;
                var pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    builder.AppendLine(pageText.Trim());
                    builder.AppendLine();
                }
            }
        }

        var text = builder.ToString().Trim();
        if (text.Length >= SparseTextThreshold || ocr is null || renderer is null || pageCount == 0)
        {
            var warnings = text.Length < SparseTextThreshold
                ? new[] { $"PDF has little or no embedded text ({pageCount} page(s)); a scanned-page OCR pass is needed for full extraction." }
                : [];
            return ParseSupport.Build(Profile, "pdf", text, text, warnings: warnings);
        }

        var cacheRoot = Path.Combine(Path.GetTempPath(), "reva-ocr-cache", Path.GetFileNameWithoutExtension(filePath));
        var ocrText = new StringBuilder();
        var spans = new List<SourceSpan>();
        var pages = new List<ParsedPage>();
        var pagesToRender = Math.Min(pageCount, MaxOcrPages);
        for (var page = 1; page <= pagesToRender; page++)
        {
            var image = await renderer.RenderPageAsync(filePath, page, cacheRoot, cancellationToken);
            pages.Add(new ParsedPage(page, image.ImagePath, image.Width, image.Height, image.Rotation));
            var result = ocr.Recognize(image.ImagePath, cancellationToken);
            foreach (var line in result.Lines.Where(line => !string.IsNullOrWhiteSpace(line.Text)))
            {
                var spanId = $"ocr-{page}-{spans.Count + 1}";
                spans.Add(new SourceSpan(spanId, Guid.Empty, page, image.Width, image.Height, image.Rotation, line.Bbox ?? new SourceBox(0, 0, 1, 1), line.Polygon, line.Text, line.Confidence, null, null, null, null));
                ocrText.AppendLine(line.Text);
            }
        }

        var merged = ocrText.ToString().Trim();
        var warningsAfterOcr = merged.Length == 0
            ? new[] { $"PDF has little or no embedded text ({pageCount} page(s)); OCR produced no readable text." }
            : Array.Empty<string>();
        return ParseSupport.Build("pdf-ocr", "pdf", merged, merged, warnings: warningsAfterOcr) with { SourceSpans = spans, Pages = pages };
    }
}
