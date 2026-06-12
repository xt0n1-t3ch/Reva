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
        foreach (var alias in FieldAliases[fieldName])
        {
            var match = FieldValueRegex(alias).Match(text);
            if (match.Success)
            {
                return new ExtractedField(fieldName, CleanValue(match.Groups[1].Value), 0.86, $"label:{alias}", false);
            }
        }

        var tableValue = ExtractFromCsvLikeText(text, fieldName);
        if (!string.IsNullOrWhiteSpace(tableValue))
        {
            return new ExtractedField(fieldName, tableValue, 0.72, "table-header", false);
        }

        return new ExtractedField(fieldName, string.Empty, 0.18, "missing", false);
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
}


