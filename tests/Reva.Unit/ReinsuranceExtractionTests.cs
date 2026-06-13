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
