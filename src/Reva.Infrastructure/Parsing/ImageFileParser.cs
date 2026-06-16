using System.Globalization;
using Reva.Core.Contracts;
using Reva.Infrastructure.Ocr;

namespace Reva.Infrastructure.Parsing;

// Raster images run through OCR. If no OCR engine is configured the router catches the
// throw and falls back to best-effort visible text, so images still produce a record.
public sealed class ImageFileParser(IOcrEngine? ocr) : IFileParser
{
    public string Profile => "image-ocr";

    public bool CanParse(string extension) =>
        extension is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp" or ".gif" or ".webp";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        if (ocr is null)
        {
            throw new InvalidOperationException("No OCR engine is configured for image parsing.");
        }

        var result = ocr.Recognize(filePath, cancellationToken);
        var width = result.Width > 0 ? result.Width : 1;
        var height = result.Height > 0 ? result.Height : 1;
        var format = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        var warnings = new List<string>();
        if (result.Text.Length == 0)
        {
            warnings.Add("OCR found no readable text in this image.");
        }
        else if (result.AverageConfidence < 0.6)
        {
            warnings.Add(string.Format(CultureInfo.InvariantCulture, "Low OCR confidence ({0:P0}); please verify the extracted text.", result.AverageConfidence));
        }

        IReadOnlyList<ParsedPage> pages = result.Width > 0 && result.Height > 0
            ? [new ParsedPage(1, filePath, width, height, 0)]
            : [];
        var spans = result.Lines.Select((line, index) => new SourceSpan($"ocr-1-{index + 1}", Guid.Empty, 1, width, height, 0, line.Bbox ?? new SourceBox(0, 0, 1, 1), line.Polygon, line.Text, line.Confidence, null, null, null, null)).ToList();
        return Task.FromResult(ParseSupport.Build(Profile, format, result.Text, result.Text, warnings: warnings) with { SourceSpans = spans, Pages = pages });
    }
}
