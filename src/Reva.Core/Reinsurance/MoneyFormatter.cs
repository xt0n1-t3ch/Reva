using System.Globalization;

namespace Reva.Core.Reinsurance;

// The single owner of money + percent presentation across the whole app (Infrastructure
// reconciliation and the Blazor cockpit both call it). No page-local money helpers.
public static class MoneyFormatter
{
    public const string DefaultCurrency = "USD";

    // "USD 5,550,000" for numeric input; the trimmed original otherwise (never throws).
    public static string Money(string? raw, string currency = DefaultCurrency) =>
        TryParseAmount(raw, out var amount) ? Money(amount, currency) : (raw ?? string.Empty).Trim();

    public static string Money(decimal amount, string currency = DefaultCurrency) =>
        string.Create(CultureInfo.InvariantCulture, $"{currency} {amount:N0}");

    // "47.72%" for numeric input; the trimmed original otherwise.
    public static string Percent(string? raw) =>
        TryParsePercent(raw, out var value) ? Percent(value) : (raw ?? string.Empty).Trim();

    public static string Percent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:0.##}%");

    // Parses "USD 5,550,000", "5550000", "1,202,500" → 5550000. Strips currency, commas, spaces.
    public static bool TryParseAmount(string? raw, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var cleaned = raw
            .Replace(DefaultCurrency, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }

    // Parses "48.10%", "47.72", "35 %" → the numeric percentage value.
    public static bool TryParsePercent(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var cleaned = raw.Replace("%", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}
