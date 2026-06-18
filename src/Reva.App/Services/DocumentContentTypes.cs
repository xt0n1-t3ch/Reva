using System;
using System.Collections.Generic;

namespace Reva.App.Services;

public static class DocumentContentTypes
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".csv"] = "text/csv",
        [".tsv"] = "text/tab-separated-values",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xlsm"] = "application/vnd.ms-excel.sheet.macroEnabled.12",
        [".xls"] = "application/vnd.ms-excel",
        [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        [".gsheet"] = "application/vnd.google-apps.spreadsheet",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".rtf"] = "application/rtf",
        [".eml"] = "message/rfc822",
        [".msg"] = "application/vnd.ms-outlook",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };
}
