namespace Reva.Core.Documents;

/// <remarks>
/// Single owner of the document extensions Reva advertises as first-class. The parser router still
/// accepts any file (unknown types degrade to a visible-text fallback); this list drives the upload
/// picker filter, settings copy, and documentation so they never drift from each other.
/// </remarks>
public static class AcceptedDocumentExtensions
{
    public static readonly IReadOnlyList<string> Spreadsheet = [".csv", ".tsv", ".xlsx", ".xlsm", ".xls", ".ods", ".gsheet"];

    public static readonly IReadOnlyList<string> Document = [".pdf", ".docx", ".pptx", ".txt", ".md"];

    public static readonly IReadOnlyList<string> Email = [".eml", ".msg"];

    public static readonly IReadOnlyList<string> Image = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp", ".gif"];

    public static readonly IReadOnlyList<string> All =
    [
        .. Spreadsheet,
        .. Document,
        .. Email,
        .. Image,
    ];

    public static bool IsAccepted(string fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
        {
            return false;
        }

        var extension = fileNameOrExtension.StartsWith('.')
            ? fileNameOrExtension
            : Path.GetExtension(fileNameOrExtension);
        return All.Contains(extension.ToLowerInvariant());
    }
}
