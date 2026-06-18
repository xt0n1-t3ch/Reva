using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class LegacyExcelFileParserTests
{
    private static readonly LegacyExcelFileParser Parser = new();

    [Theory]
    [InlineData(".xls")]
    public void CanParseReturnsTrueForXls(string extension)
    {
        Assert.True(Parser.CanParse(extension));
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".csv")]
    [InlineData(".ods")]
    [InlineData(".txt")]
    [InlineData("")]
    public void CanParseReturnsFalseForOtherExtensions(string extension)
    {
        Assert.False(Parser.CanParse(extension));
    }

    [Fact]
    public void ProfileIsExcelLegacy()
    {
        Assert.Equal("excel-legacy", Parser.Profile);
    }

    [Fact]
    public async Task RouterFallsBackWithWarningWhenXlsBytesAreGarbage()
    {
        var path = TempPath(".xls");
        try
        {
            await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE]);

            var router = new ParserRouter();
            var parsed = await router.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RouterFallsBackWithWarningWhenXlsFileIsEmpty()
    {
        var path = TempPath(".xls");
        try
        {
            await File.WriteAllBytesAsync(path, []);

            var router = new ParserRouter();
            var parsed = await router.ParseAsync(path, CancellationToken.None);

            Assert.NotEmpty(parsed.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RouterSelectsExcelLegacyParserForXlsExtension()
    {
        var path = TempPath(".xls");
        try
        {
            await File.WriteAllBytesAsync(path, [0xD0, 0xCF, 0x11, 0xE0]);

            var router = new ParserRouter();
            var parsed = await router.ParseAsync(path, CancellationToken.None);

            Assert.NotNull(parsed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"reva-test-{Guid.NewGuid():N}{extension}");
}
