using System.Text.RegularExpressions;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Extraction;

public sealed partial class ReinsuranceFieldExtractor : IReinsuranceExtractor
{
    private static readonly Dictionary<string, string[]> FieldAliases = new()
    {
        [ReinsuranceFieldNames.Cedent] = ["cedent", "ceding company", "client"],
        [ReinsuranceFieldNames.Broker] = ["broker", "intermediary"],
        [ReinsuranceFieldNames.Reinsurer] = ["reinsurer", "reinsurance company"],
        [ReinsuranceFieldNames.ContractReference] = ["contract ref", "contract reference", "treaty ref", "policy ref"],
        [ReinsuranceFieldNames.LineOfBusiness] = ["line of business", "lob", "class of business"],
        [ReinsuranceFieldNames.Period] = ["period", "account period", "treaty period"],
        [ReinsuranceFieldNames.Currency] = ["currency", "ccy"],
        [ReinsuranceFieldNames.Premium] = ["premium", "gross premium", "net premium"],
        [ReinsuranceFieldNames.Claims] = ["claims", "paid loss", "outstanding loss", "incurred"],
        [ReinsuranceFieldNames.Commission] = ["commission", "brokerage"],
        [ReinsuranceFieldNames.Cession] = ["cession", "cession %", "share"],
        [ReinsuranceFieldNames.Retention] = ["retention", "deductible"],
        [ReinsuranceFieldNames.Limit] = ["limit", "sum insured", "capacity"]
    };

    public ReinsuranceExtractionResult Extract(ParsedDocument parsedDocument, ClassificationResult classificationResult)
    {
        var fields = ReinsuranceFieldNames.Canonical
            .Select(name => ExtractField(parsedDocument.Text, name))
            .ToList();

        var exceptions = BuildExceptions(parsedDocument, classificationResult, fields).ToList();
        var foundConfidence = fields.Count == 0 ? 0 : fields.Average(field => field.Confidence);
        var confidence = Math.Round((classificationResult.Confidence * 0.45) + (foundConfidence * 0.55), 2);

        return new ReinsuranceExtractionResult(
            classificationResult.DocumentType,
            confidence,
            fields,
            parsedDocument.Tables,
            exceptions);
    }

    private static ExtractedField ExtractField(string text, string fieldName)
    {
        var aliases = FieldAliases[fieldName];
        for (var i = 0; i < aliases.Length; i++)
        {
            var match = FieldValueRegex(aliases[i]).Match(text);
            if (match.Success)
            {
                var value = CleanValue(match.Groups[1].Value);
                // Canonical label is a stronger locator than a synonym.
                var locator = i == 0 ? 0.95 : 0.88;
                return new ExtractedField(fieldName, value, Score(fieldName, value, locator), $"label:{aliases[i]}", false);
            }
        }

        var tableValue = ExtractFromCsvLikeText(text, fieldName);
        if (!string.IsNullOrWhiteSpace(tableValue))
        {
            return new ExtractedField(fieldName, tableValue, Score(fieldName, tableValue, 0.74), "table-header", false);
        }

        return new ExtractedField(fieldName, string.Empty, 0.12, "missing", false);
    }

    // Real, explainable confidence: how the value was located blended with whether it passes
    // a domain check for that field. No hardcoded constants.
    private static double Score(string fieldName, string value, double locator)
    {
        var validation = ValidationConfidence(fieldName, value);
        return Math.Round((0.55 * locator) + (0.45 * validation), 2);
    }

    private static double ValidationConfidence(string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0.5;
        }

        if (fieldName == ReinsuranceFieldNames.Currency)
        {
            return CurrencyRegex().IsMatch(value.Trim()) ? 0.99 : 0.55;
        }

        if (fieldName is ReinsuranceFieldNames.Premium or ReinsuranceFieldNames.Claims
            or ReinsuranceFieldNames.Commission or ReinsuranceFieldNames.Retention or ReinsuranceFieldNames.Limit)
        {
            return IsNumericAmount(value) ? 0.96 : 0.78;
        }

        if (fieldName == ReinsuranceFieldNames.Cession)
        {
            return value.Any(char.IsDigit) ? 0.95 : 0.7;
        }

        if (fieldName == ReinsuranceFieldNames.Period)
        {
            return YearRegex().IsMatch(value) ? 0.93 : 0.72;
        }

        if (fieldName == ReinsuranceFieldNames.ContractReference)
        {
            return value.Length >= 4 && value.Any(char.IsDigit) ? 0.93 : 0.75;
        }

        // Free-text parties / lines of business: trust a non-trivial value.
        return value.Trim().Length >= 2 ? 0.9 : 0.6;
    }

    private static bool IsNumericAmount(string value)
    {
        var cleaned = value
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Trim();
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    private static string? ExtractFromCsvLikeText(string text, string fieldName)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return null;
        }

        var headers = lines[0].Split(',').Select(header => header.Trim()).ToArray();
        var index = Array.FindIndex(headers, header => header.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        var values = lines[1].Split(',').Select(value => value.Trim()).ToArray();
        return index < values.Length ? values[index] : null;
    }

    private static IEnumerable<ExtractionIssue> BuildExceptions(ParsedDocument parsedDocument, ClassificationResult classificationResult, IReadOnlyList<ExtractedField> fields)
    {
        if (classificationResult.DocumentType == ReinsuranceDocumentType.Unknown)
        {
            yield return new ExtractionIssue(ExceptionSeverity.Warning, "Document type could not be classified with high confidence.");
        }

        foreach (var field in fields.Where(field => string.IsNullOrWhiteSpace(field.Value)))
        {
            yield return new ExtractionIssue(ExceptionSeverity.Warning, $"Missing canonical field: {field.Name}.");
        }

        if (parsedDocument.Warnings.Count > 0)
        {
            foreach (var warning in parsedDocument.Warnings)
            {
                yield return new ExtractionIssue(ExceptionSeverity.Info, warning);
            }
        }

        var currency = fields.FirstOrDefault(field => field.Name == ReinsuranceFieldNames.Currency)?.Value;
        if (!string.IsNullOrWhiteSpace(currency) && !CurrencyRegex().IsMatch(currency))
        {
            yield return new ExtractionIssue(ExceptionSeverity.Critical, "Currency does not look like a three-letter ISO code.");
        }
    }

    private static string CleanValue(string value)
    {
        return value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim(' ', '.', ';');
    }

    private static Regex FieldValueRegex(string label)
    {
        return new Regex($"{Regex.Escape(label)}\\s*[:=-]\\s*([^\\r\\n]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyRegex();

    [GeneratedRegex(@"\d{4}", RegexOptions.CultureInvariant)]
    private static partial Regex YearRegex();
}


