using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.SchemaMapping;

public sealed partial class SchemaMappingService(RevaDbContext dbContext) : ISchemaMappingService
{
    public const string UnknownSender = "unknown";

    private static readonly IReadOnlyDictionary<string, string[]> StaticAliases = new Dictionary<string, string[]>
    {
        [ReinsuranceFieldNames.Cedent] = ["cedent", "cedant", "cedant co", "cedant company", "ceding company", "client", "insured company"],
        [ReinsuranceFieldNames.Broker] = ["broker", "intermediary", "placing broker"],
        [ReinsuranceFieldNames.Reinsurer] = ["reinsurer", "market", "carrier", "security"],
        [ReinsuranceFieldNames.ContractReference] = ["contract ref", "contract reference", "treaty ref", "policy ref", "umr", "unique market reference", "contract id"],
        [ReinsuranceFieldNames.LineOfBusiness] = ["line of business", "lob", "class of business", "business class", "risk class"],
        [ReinsuranceFieldNames.Period] = ["period", "account period", "treaty period", "period of cover", "effective date", "inception date"],
        [ReinsuranceFieldNames.Currency] = ["currency", "ccy", "original currency", "settlement currency"],
        [ReinsuranceFieldNames.Premium] = ["premium", "gross premium", "gross written premium", "gwp", "premium gross", "premium_gross", "written premium"],
        [ReinsuranceFieldNames.Claims] = ["claims", "paid loss", "paid losses", "outstanding loss", "incurred", "claim amount", "loss amount"],
        [ReinsuranceFieldNames.Commission] = ["commission", "brokerage", "ceding commission", "comm"],
        [ReinsuranceFieldNames.Cession] = ["cession", "cession %", "share", "signed share", "written share", "participation"],
        [ReinsuranceFieldNames.Retention] = ["retention", "deductible", "excess", "attachment"],
        [ReinsuranceFieldNames.Limit] = ["limit", "sum insured", "capacity", "policy limit", "layer limit"]
    };

    private static readonly Dictionary<string, string> CurrencyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$"] = "USD",
        ["usd"] = "USD",
        ["us dollar"] = "USD",
        ["us dollars"] = "USD",
        ["dollar"] = "USD",
        ["dollars"] = "USD",
        ["gbp"] = "GBP",
        ["pound"] = "GBP",
        ["pounds"] = "GBP",
        ["sterling"] = "GBP",
        ["eur"] = "EUR",
        ["euro"] = "EUR",
        ["euros"] = "EUR"
    };

    public async Task<SchemaMappingResult> MapAsync(ParsedDocument parsedDocument, IReadOnlyList<ExtractedField> fields, CancellationToken cancellationToken)
    {
        var senderKey = DetectSenderKey(parsedDocument.Text);
        var learned = await dbContext.LearnedSchemaMappings
            .AsNoTracking()
            .Where(mapping => mapping.SenderKey == senderKey)
            .ToDictionaryAsync(mapping => mapping.NormalizedSourceHeader, StringComparer.Ordinal, cancellationToken);

        var mappings = BuildMappings(parsedDocument.Tables, senderKey, learned);
        var enrichedFields = ApplyMappings(fields, mappings);
        return new SchemaMappingResult(enrichedFields, mappings);
    }

    public async Task LearnAsync(string senderKey, IReadOnlyList<SchemaMappingCorrection> corrections, CancellationToken cancellationToken)
    {
        if (corrections.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var correction in corrections.Where(IsValidCorrection))
        {
            var normalized = NormalizeHeader(correction.SourceHeader);
            var existing = await dbContext.LearnedSchemaMappings
                .FirstOrDefaultAsync(mapping => mapping.SenderKey == senderKey && mapping.NormalizedSourceHeader == normalized, cancellationToken);
            if (existing is null)
            {
                dbContext.LearnedSchemaMappings.Add(new LearnedSchemaMappingRecord
                {
                    SenderKey = senderKey,
                    SourceHeader = correction.SourceHeader.Trim(),
                    NormalizedSourceHeader = normalized,
                    CanonicalField = correction.CanonicalField,
                    UseCount = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                continue;
            }

            existing.SourceHeader = correction.SourceHeader.Trim();
            existing.CanonicalField = correction.CanonicalField;
            existing.UseCount++;
            existing.UpdatedAt = now;
        }
    }

    public static string DetectSenderKey(string text)
    {
        var match = FromLineRegex().Match(text);
        if (!match.Success)
        {
            return UnknownSender;
        }

        var sender = match.Groups[1].Value.Trim();
        var email = EmailRegex().Match(sender);
        if (email.Success)
        {
            return email.Groups[1].Value.ToLowerInvariant();
        }

        var normalized = NormalizeHeader(sender);
        return string.IsNullOrWhiteSpace(normalized) ? UnknownSender : normalized;
    }

    public static string NormalizeHeader(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var normalized = NonAlphaNumericRegex().Replace(lowered, " ").Trim();
        return MultiSpaceRegex().Replace(normalized, " ");
    }

    private static List<DocumentSchemaMappingRecord> BuildMappings(
        IReadOnlyList<ExtractedTable> tables,
        string senderKey,
        IReadOnlyDictionary<string, LearnedSchemaMappingRecord> learned)
    {
        var mappings = new List<DocumentSchemaMappingRecord>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            foreach (var header in table.Headers.Where(header => !string.IsNullOrWhiteSpace(header)))
            {
                var normalized = NormalizeHeader(header);
                if (!seen.Add(normalized))
                {
                    continue;
                }

                var (canonical, confidence, source, isLearned) = ResolveHeader(header, normalized, learned);
                mappings.Add(new DocumentSchemaMappingRecord
                {
                    SenderKey = senderKey,
                    SourceHeader = header.Trim(),
                    NormalizedSourceHeader = normalized,
                    CanonicalField = canonical,
                    NormalizedValue = NormalizeValue(canonical, FirstValue(table, header)),
                    Confidence = confidence,
                    Source = source,
                    IsLearned = isLearned
                });
            }
        }

        return mappings;
    }

    private static List<ExtractedField> ApplyMappings(IReadOnlyList<ExtractedField> fields, IReadOnlyList<DocumentSchemaMappingRecord> mappings)
    {
        var merged = fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in mappings.Where(mapping => !string.IsNullOrWhiteSpace(mapping.CanonicalField) && !string.IsNullOrWhiteSpace(mapping.NormalizedValue)))
        {
            if (!merged.TryGetValue(mapping.CanonicalField, out var existing)
                || string.IsNullOrWhiteSpace(existing.Value)
                || existing.Confidence < 0.6)
            {
                merged[mapping.CanonicalField] = new ExtractedField(
                    mapping.CanonicalField,
                    mapping.NormalizedValue,
                    Math.Round(Math.Min(mapping.Confidence, 0.95), 2),
                    $"schema:{mapping.SourceHeader}",
                    false);
            }
        }

        return ReinsuranceFieldNames.Canonical
            .Select(field => merged.TryGetValue(field, out var value) ? value : new ExtractedField(field, string.Empty, 0.12, "missing", false))
            .ToList();
    }

    private static (string CanonicalField, double Confidence, string Source, bool IsLearned) ResolveHeader(
        string header,
        string normalized,
        IReadOnlyDictionary<string, LearnedSchemaMappingRecord> learned)
    {
        if (learned.TryGetValue(normalized, out var learnedMapping))
        {
            return (learnedMapping.CanonicalField, 0.99, "learned", true);
        }

        foreach (var (canonical, aliases) in StaticAliases)
        {
            if (aliases.Select(NormalizeHeader).Any(alias => alias == normalized))
            {
                return (canonical, 0.94, "alias", false);
            }
        }

        var best = StaticAliases
            .SelectMany(pair => pair.Value.Select(alias => (Canonical: pair.Key, Score: Similarity(normalized, NormalizeHeader(alias)))))
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();

        if (best.Score >= 0.72)
        {
            return (best.Canonical, Math.Round(0.62 + (best.Score * 0.24), 2), "fuzzy", false);
        }

        return (string.Empty, 0.15, "unmapped", false);
    }

    private static string FirstValue(ExtractedTable table, string header)
    {
        foreach (var row in table.Rows)
        {
            if (row.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string NormalizeValue(string canonicalField, string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(canonicalField) || string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        if (canonicalField == ReinsuranceFieldNames.Currency)
        {
            return CurrencyAliases.TryGetValue(trimmed.Trim(), out var currency) ? currency : trimmed.ToUpperInvariant();
        }

        if (canonicalField is ReinsuranceFieldNames.Premium or ReinsuranceFieldNames.Claims
            or ReinsuranceFieldNames.Commission or ReinsuranceFieldNames.Retention or ReinsuranceFieldNames.Limit)
        {
            return MoneyFormatter.Money(trimmed);
        }

        if (canonicalField == ReinsuranceFieldNames.Cession)
        {
            return MoneyFormatter.Percent(trimmed);
        }

        if (canonicalField == ReinsuranceFieldNames.Period
            && DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return StatusRegex().IsMatch(trimmed) ? NormalizeHeader(trimmed).ToUpperInvariant().Replace(' ', '_') : trimmed;
    }

    private static double Similarity(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var intersection = leftTokens.Intersect(rightTokens, StringComparer.Ordinal).Count();
        var union = leftTokens.Union(rightTokens, StringComparer.Ordinal).Count();
        var tokenScore = union == 0 ? 0 : (double)intersection / union;
        var distance = Levenshtein(left, right);
        var editScore = 1d - ((double)distance / Math.Max(left.Length, right.Length));
        return Math.Max(tokenScore, editScore);
    }

    private static int Levenshtein(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static bool IsValidCorrection(SchemaMappingCorrection correction) =>
        !string.IsNullOrWhiteSpace(correction.SourceHeader)
        && ReinsuranceFieldNames.Canonical.Contains(correction.CanonicalField, StringComparer.Ordinal);

    [GeneratedRegex(@"^From:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FromLineRegex();

    [GeneratedRegex(@"@([A-Za-z0-9.-]+\.[A-Za-z]{2,})", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"^(open|closed|settled|pending|paid|denied|active|expired)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatusRegex();
}
