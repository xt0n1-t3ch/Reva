using System.Globalization;
using System.IO.Compression;
using System.Text;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class OpenDocumentSpreadsheetParserTests
{
    private static readonly OpenDocumentSpreadsheetParser Parser = new();

    [Fact]
    public void CanParseReturnsTrueForOds()
    {
        Assert.True(Parser.CanParse(".ods"));
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".csv")]
    [InlineData(".txt")]
    [InlineData("")]
    public void CanParseReturnsFalseForOtherExtensions(string extension)
    {
        Assert.False(Parser.CanParse(extension));
    }

    [Fact]
    public void ProfileIsOds()
    {
        Assert.Equal("ods", Parser.Profile);
    }

    [Fact]
    public async Task ParseAsyncExtractsHeadersAndDataRows()
    {
        var path = TempPath(".ods");
        try
        {
            var contentXml = BuildContentXml(
                "Bordereau",
                ["Cedent", "Premium"],
                [["Orion Insurance", "5550000"], ["Halcyon Mutual", "2100000"]]);

            WriteOdsZip(path, contentXml);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("ods", parsed.ParserProfile);
            var table = Assert.Single(parsed.Tables);
            Assert.Contains("Cedent", table.Headers);
            Assert.Contains("Premium", table.Headers);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("Orion Insurance", table.Rows[0]["Cedent"]);
            Assert.Equal("5550000", table.Rows[0]["Premium"]);
            Assert.Equal("Halcyon Mutual", table.Rows[1]["Cedent"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncEmptySheetProducesWarning()
    {
        var path = TempPath(".ods");
        try
        {
            var contentXml = BuildContentXml("EmptySheet", [], []);
            WriteOdsZip(path, contentXml);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
            Assert.Empty(parsed.Tables);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncNonZipBytesProducesWarning()
    {
        var path = TempPath(".ods");
        try
        {
            await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0x03]);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
            Assert.Empty(parsed.Tables);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncEmptyFileProducesWarning()
    {
        var path = TempPath(".ods");
        try
        {
            await File.WriteAllBytesAsync(path, []);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
            Assert.Empty(parsed.Tables);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncZipWithoutContentXmlProducesWarning()
    {
        var path = TempPath(".ods");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("manifest.xml");
                using var stream = entry.Open();
                stream.WriteByte(0);
            }

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
            Assert.Empty(parsed.Tables);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncMalformedXmlProducesWarning()
    {
        var path = TempPath(".ods");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("content.xml");
                using var stream = entry.Open();
                var malformed = Encoding.UTF8.GetBytes("<<not xml>>");
                stream.Write(malformed);
            }

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
            Assert.Empty(parsed.Tables);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncIncludesMarkdownTableInOutput()
    {
        var path = TempPath(".ods");
        try
        {
            var contentXml = BuildContentXml(
                "Sheet1",
                ["Name", "Value"],
                [["Alpha", "100"]]);

            WriteOdsZip(path, contentXml);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.Contains("Name", parsed.Markdown, StringComparison.Ordinal);
            Assert.Contains("Alpha", parsed.Markdown, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncHandlesHeaderOnlySheet()
    {
        var path = TempPath(".ods");
        try
        {
            var contentXml = BuildContentXml("HeaderOnly", ["A", "B"], []);
            WriteOdsZip(path, contentXml);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("ods", parsed.ParserProfile);
            var table = Assert.Single(parsed.Tables);
            Assert.Equal(["A", "B"], table.Headers);
            Assert.Empty(table.Rows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string BuildContentXml(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> dataRows)
    {
        const string TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
        const string TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
        const string OfficeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

        var sb = new StringBuilder();
        sb.AppendLine($"""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine($"""<office:document-content xmlns:office="{OfficeNs}" xmlns:table="{TableNs}" xmlns:text="{TextNs}">""");
        sb.AppendLine("<office:body><office:spreadsheet>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"""<table:table table:name="{sheetName}">""");

        if (headers.Count > 0)
        {
            sb.AppendLine("<table:table-row>");
            foreach (var header in headers)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"<table:table-cell><text:p>{header}</text:p></table:table-cell>");
            }

            sb.AppendLine("</table:table-row>");
        }

        foreach (var row in dataRows)
        {
            sb.AppendLine("<table:table-row>");
            foreach (var cell in row)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"<table:table-cell><text:p>{cell}</text:p></table:table-cell>");
            }

            sb.AppendLine("</table:table-row>");
        }

        sb.AppendLine("</table:table>");
        sb.AppendLine("</office:spreadsheet></office:body>");
        sb.AppendLine("</office:document-content>");
        return sb.ToString();
    }

    private static void WriteOdsZip(string path, string contentXml)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("content.xml");
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(contentXml);
        stream.Write(bytes);
    }

    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"reva-test-{Guid.NewGuid():N}{extension}");
}
