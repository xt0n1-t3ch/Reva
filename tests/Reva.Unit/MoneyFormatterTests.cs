using Reva.Core.Reinsurance;

namespace Reva.Unit;

public sealed class MoneyFormatterTests
{
    [Theory]
    [InlineData("5550000", "USD 5,550,000")]
    [InlineData("USD 5,550,000", "USD 5,550,000")]
    [InlineData("2,450,000", "USD 2,450,000")]
    [InlineData("$1200000", "USD 1,200,000")]
    public void MoneyFormatsNumericValuesWithGroupingAndCurrency(string raw, string expected)
    {
        Assert.Equal(expected, MoneyFormatter.Money(raw));
    }

    [Theory]
    [InlineData("Property Cat XL")]
    [InlineData("")]
    public void MoneyLeavesNonNumericValuesUntouched(string raw)
    {
        Assert.Equal(raw, MoneyFormatter.Money(raw));
    }

    [Fact]
    public void MoneyUsesInvariantGroupingRegardlessOfCulture()
    {
        // No locale-specific separators leak in (CA1305 contract): always comma-grouped.
        Assert.Equal("USD 1,000,000", MoneyFormatter.Money(1_000_000m));
    }

    [Theory]
    [InlineData("48.10%", 48.10)]
    [InlineData("47.72", 47.72)]
    [InlineData("35 %", 35)]
    public void TryParsePercentReadsRates(string raw, double expected)
    {
        Assert.True(MoneyFormatter.TryParsePercent(raw, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Fact]
    public void TryParseAmountRejectsNonNumeric()
    {
        Assert.False(MoneyFormatter.TryParseAmount("Orion Insurance", out _));
    }
}
