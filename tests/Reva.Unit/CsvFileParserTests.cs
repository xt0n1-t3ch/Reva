using System.Text;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class CsvFileParserTests
{
    private static readonly CsvFileParser Parser = new();

    [Theory]
    [InlineData(".csv")]
    [InlineData(".tsv")]
    public void CanParseReturnsTrueForCsvAndTsv(string extension)
    {
        Assert.True(Parser.CanParse(extension));
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".txt")]
    [InlineData("")]
    public void CanParseReturnsFalseForOtherExtensions(string extension)
    {
        Assert.False(Parser.CanParse(extension));
    }

    [Fact]
    public void ProfileIsCsv()
    {
        Assert.Equal("csv", Parser.Profile);
    }

    [Fact]
    public async Task ParseAsyncDetectsCommaDelimitedCsv()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Cedent,Premium\nOrion Insurance,5550000\nHalcyon Mutual,2100000\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("csv", parsed.ParserProfile);
            Assert.Equal("csv", parsed.SourceFormat);
            var table = Assert.Single(parsed.Tables);
            Assert.Contains("Cedent", table.Headers);
            Assert.Contains("Premium", table.Headers);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("Orion Insurance", table.Rows[0]["Cedent"]);
            Assert.Equal("5550000", table.Rows[0]["Premium"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncDetectsTabDelimitedCsvByContent()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Cedent\tPremium\nOrion Insurance\t5550000\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            var table = Assert.Single(parsed.Tables);
            Assert.Contains("Cedent", table.Headers);
            Assert.Equal("Orion Insurance", table.Rows[0]["Cedent"]);
            Assert.Equal("5550000", table.Rows[0]["Premium"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncDetectsSemicolonDelimitedCsvByContent()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Cedent;Premium;Currency\nOrion;5550000;USD\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            var table = Assert.Single(parsed.Tables);
            Assert.Contains("Currency", table.Headers);
            Assert.Equal("USD", table.Rows[0]["Currency"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncTsvExtensionAlwaysUsesTab()
    {
        var path = TempPath(".tsv");
        try
        {
            await File.WriteAllTextAsync(path, "Cedent\tPremium\nOrion Insurance\t5550000\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("tsv", parsed.SourceFormat);
            var table = Assert.Single(parsed.Tables);
            Assert.Equal("Orion Insurance", table.Rows[0]["Cedent"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncTsvWithCommasInCellsDoesNotSplitOnComma()
    {
        var path = TempPath(".tsv");
        try
        {
            await File.WriteAllTextAsync(path, "Name\tAddress\nOrion\t100 Main St, Suite 5\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            var table = Assert.Single(parsed.Tables);
            Assert.Equal("100 Main St, Suite 5", table.Rows[0]["Address"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncEmptyCsvProducesWarning()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, string.Empty, Encoding.UTF8);

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
    public async Task ParseAsyncEmptyTsvProducesWarning()
    {
        var path = TempPath(".tsv");
        try
        {
            await File.WriteAllTextAsync(path, string.Empty, Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncHeaderOnlyRowProducesEmptyTable()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Cedent,Premium\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            var table = Assert.Single(parsed.Tables);
            Assert.Empty(table.Rows);
            Assert.Contains("Cedent", table.Headers);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncPopulatesRawTextAndMarkdown()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Name,Value\nAlpha,100\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Text);
            Assert.Contains("Name", parsed.Markdown, StringComparison.Ordinal);
            Assert.Contains("Alpha", parsed.Markdown, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsyncQuotedCsvFieldsWithEmbeddedCommasAreHandled()
    {
        var path = TempPath(".csv");
        try
        {
            await File.WriteAllTextAsync(path, "Name,Description\nOrion,\"Large, diversified carrier\"\n", Encoding.UTF8);

            var parsed = await Parser.ParseAsync(path, CancellationToken.None);

            var table = Assert.Single(parsed.Tables);
            Assert.Equal("Large, diversified carrier", table.Rows[0]["Description"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"reva-test-{Guid.NewGuid():N}{extension}");
}
