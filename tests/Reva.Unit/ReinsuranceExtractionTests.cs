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
