using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class ReinsuranceExtractionTests
{
    [Fact]
    public void ClassifierDetectsTechnicalAccountStatement()
    {
        var parsed = new ParsedDocument(
            "test",
            "txt",
            SampleStatementText(),
            SampleStatementText(),
            "{}",
            [],
            []);

        var result = new ReinsuranceClassifier().Classify(parsed);

        Assert.Equal(ReinsuranceDocumentType.StatementOfAccount, result.DocumentType);
        Assert.True(result.Confidence > 0.6);
    }

    [Fact]
    public void ExtractorReturnsCanonicalFieldsAndIssues()
    {
        var parsed = new ParsedDocument(
            "test",
            "txt",
            SampleStatementText(),
            SampleStatementText(),
            "{}",
            [],
            []);
        var classifier = new ReinsuranceClassifier();
        var extractor = new ReinsuranceFieldExtractor();

        var result = extractor.Extract(parsed, classifier.Classify(parsed));

        Assert.Equal(ReinsuranceDocumentType.StatementOfAccount, result.DocumentType);
        Assert.Contains(result.Fields, field => field.Name == ReinsuranceFieldNames.Cedent && field.Value == "Andes Mutual Insurance");
        Assert.Contains(result.Fields, field => field.Name == ReinsuranceFieldNames.Currency && field.Value == "USD");
        Assert.DoesNotContain(result.Exceptions, issue => issue.Severity == ExceptionSeverity.Critical);
    }

    [Fact]
    public void ClassifierDetectsBordereauFromTabularColumns()
    {
        // A bordereau CSV: generic words ("commission","premium") also appear in statements,
        // but the row-per-risk table with reinsurance columns should win.
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string> { ["Cedent"] = "Orion", ["Premium"] = "5550000", ["Cession %"] = "47.72%" }
        ];
        var table = new ExtractedTable("bordereau", ["Cedent", "Premium", "Claims", "Commission", "Cession %"], rows);
        var text = "Cedent,Premium,Claims,Commission,Cession %\nOrion,5550000,2625000,277500,47.72%";
        var parsed = new ParsedDocument("test", "csv", text, text, "{}", [table], []);

        var result = new ReinsuranceClassifier().Classify(parsed);

        Assert.Equal(ReinsuranceDocumentType.Bordereau, result.DocumentType);
    }

    [Fact]
    public void ExtractorProducesVariedRealConfidence()
    {
        var text = "Cedent: Orion Insurance\nCurrency: USD";
        var parsed = new ParsedDocument("test", "txt", text, text, "{}", [], []);
        var extractor = new ReinsuranceFieldExtractor();

        var result = extractor.Extract(parsed, new ReinsuranceClassifier().Classify(parsed));
        var found = result.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)).ToList();
        var missing = result.Fields.Where(field => string.IsNullOrWhiteSpace(field.Value)).ToList();

        // Confidence is computed, not a flat constant.
        Assert.True(found.Select(field => field.Confidence).Distinct().Count() > 1, "found fields should carry varied confidence");
        Assert.All(found, field => Assert.True(field.Confidence > 0.6, $"{field.Name} confidence {field.Confidence}"));
        Assert.All(missing, field => Assert.True(field.Confidence < 0.3, $"{field.Name} confidence {field.Confidence}"));
    }

    [Fact]
    public void ExtractorReconcilesStatedTotalsAgainstLineItems()
    {
        // A broker cover note states a Premium that disagrees with the attached line items.
        // Reconciliation must surface a field-level exception with both values and a real score.
        var text = "Cedent: Orion Insurance Company Ltd.\nCurrency: USD\nPremium: USD 4,400,000\n";
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string> { ["Member"] = "North America", ["Premium"] = "2450000" },
            new Dictionary<string, string> { ["Member"] = "Europe", ["Premium"] = "1850000" },
            new Dictionary<string, string> { ["Member"] = "Asia Pacific", ["Premium"] = "1250000" }
        ];
        var table = new ExtractedTable("bordereau", ["Member", "Premium"], rows);
        var parsed = new ParsedDocument("test", "csv", text, text, "{}", [table], []);
        var extractor = new ReinsuranceFieldExtractor();

        var result = extractor.Extract(parsed, new ReinsuranceClassifier().Classify(parsed));

        var premiumBreak = result.Exceptions.Single(issue =>
            issue.IsReconciliation && issue.FieldName == ReinsuranceFieldNames.Premium);
        Assert.Equal("USD 4,400,000", premiumBreak.Detected);
        // Expected is the computed line-item total (5,550,000), never the stated value.
        Assert.Equal("USD 5,550,000", premiumBreak.Expected);
        Assert.InRange(premiumBreak.Confidence, 0.01, 0.99);
    }

    [Fact]
    public void ExtractorRaisesNoReconciliationWhenStatedMatchesLineItems()
    {
        var text = "Currency: USD\nPremium: USD 5,550,000\n";
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string> { ["Premium"] = "2450000" },
            new Dictionary<string, string> { ["Premium"] = "3100000" }
        ];
        var table = new ExtractedTable("bordereau", ["Premium"], rows);
        var parsed = new ParsedDocument("test", "csv", text, text, "{}", [table], []);
        var extractor = new ReinsuranceFieldExtractor();

        var result = extractor.Extract(parsed, new ReinsuranceClassifier().Classify(parsed));

        Assert.DoesNotContain(result.Exceptions, issue =>
            issue.IsReconciliation && issue.FieldName == ReinsuranceFieldNames.Premium);
    }

    private static string SampleStatementText()
    {
        return """
            Cedent: Andes Mutual Insurance
            Broker: Meridian Re Brokers
            Reinsurer: Active Capital Reinsurance
            Contract Ref: AR-TREATY-2026-0042
            Line of Business: Property & Engineering
            Period: 2026-01-01 to 2026-03-31
            Currency: USD
            Premium: 1200000
            Claims: 245000
            Commission: 180000
            Cession: 35%
            Retention: 500000
            Limit: 5000000
            Technical Account Statement
            """;
    }
}
