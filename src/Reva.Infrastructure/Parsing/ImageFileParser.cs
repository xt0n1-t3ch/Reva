using System.Globalization;
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

        return Task.FromResult(ParseSupport.Build(Profile, format, result.Text, result.Text, warnings: warnings));
    }
}
