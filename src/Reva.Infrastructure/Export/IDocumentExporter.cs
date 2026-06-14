using Reva.Core.Contracts;

namespace Reva.Infrastructure.Export;

public interface IDocumentExporter
{
    // A small rendered sample (headers + first rows) for the live preview.
    ExportPreview Preview(DocumentDetail document, ExportTemplate layout, int maxRows = 6);

    // The full export rendered to bytes, ready to download.
    ExportFile Export(DocumentDetail document, ExportTemplate layout);
}
