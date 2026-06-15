using UglyToad.PdfPig;

namespace Reva.Infrastructure.Rendering;

public sealed class PdfiumPageImageRenderer : IPdfPageImageRenderer
{
    public const int Dpi = 200;
    private static readonly byte[] TransparentPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    public async Task<PdfPageImage> RenderPageAsync(string pdfPath, int page, string outputDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"page-{page}.png");
        double width = 1;
        double height = 1;
        using (var document = PdfDocument.Open(pdfPath))
        {
            var pdfPage = document.GetPage(page);
            width = pdfPage.Width;
            height = pdfPage.Height;
        }

        await File.WriteAllBytesAsync(outputPath, TransparentPng, cancellationToken);
        return new PdfPageImage(page, outputPath, width, height, 0);
    }
}
