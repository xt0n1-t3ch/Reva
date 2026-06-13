using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Reva.Infrastructure.Parsing;

// Digital PDFs (with a real text layer) via PdfPig. Scanned PDFs with little or no text
// are flagged here and handed to OCR in a later phase.
public sealed class PdfFileParser : IFileParser
{
    private const int SparseTextThreshold = 24;

    public string Profile => "pdf-text";

    public bool CanParse(string extension) => extension == ".pdf";

    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
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
        var warnings = text.Length < SparseTextThreshold
            ? new[] { $"PDF has little or no embedded text ({pageCount} page(s)); a scanned-page OCR pass is needed for full extraction." }
            : [];

        return Task.FromResult(ParseSupport.Build(Profile, "pdf", text, text, warnings: warnings));
    }
}
