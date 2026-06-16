namespace Reva.Infrastructure.Rendering;

public sealed record PdfPageImage(int Page, string ImagePath, double Width, double Height, int Rotation);

public interface IPdfPageImageRenderer
{
    Task<PdfPageImage> RenderPageAsync(string pdfPath, int page, string outputDirectory, CancellationToken cancellationToken);
}
