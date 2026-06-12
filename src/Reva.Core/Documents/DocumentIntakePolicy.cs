namespace Reva.Core.Documents;

public static class DocumentIntakePolicy
{
    public const long MaxFileBytes = 15 * 1024 * 1024;

    public static readonly string[] SupportedExtensions =
    [
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".tif",
        ".tiff",
        ".txt",
        ".csv",
        ".md"
    ];
}

