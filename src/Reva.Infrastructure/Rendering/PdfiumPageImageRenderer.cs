using PDFtoImage;
using UglyToad.PdfPig;

namespace Reva.Infrastructure.Rendering;

public sealed class PdfiumPageImageRenderer : IPdfPageImageRenderer
{
    public const int Dpi = 200;

    public async Task<PdfPageImage> RenderPageAsync(string pdfPath, int page, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"page-{page}.png");
        double width;
        double height;
        using (var document = PdfDocument.Open(pdfPath))
        {
            var pdfPage = document.GetPage(page);
            width = pdfPage.Width;
            height = pdfPage.Height;
        }

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        var options = new RenderOptions { Dpi = Dpi, UseTiling = true };
#pragma warning disable CA1416
        await Task.Run(() => Conversion.SavePng(outputPath, pdfBytes, page - 1, null, options), cancellationToken);
#pragma warning restore CA1416
        cancellationToken.ThrowIfCancellationRequested();

        return new PdfPageImage(page, outputPath, width, height, 0);
    }
}
