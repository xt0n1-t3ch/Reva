using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Extraction;

namespace Reva.Unit;

public sealed class LlmExtractionTests
{
    [Fact]
    public void NullProposalKeepsDeterministicBaseline()
    {
        var baseline = Baseline();
        var merged = new ExtractionMerger().Merge(baseline, null);
        Assert.Equal(baseline.Fields, merged.Fields);
    }

    [Fact]
    public void CitedProposalCanFillMissingTextButCannotOverwriteMoney()
    {
        var baseline = Baseline();
        var proposal = new LlmFieldProposal([
            new ExtractedField(ReinsuranceFieldNames.Broker, "Alt Broker", 0.91, "llm-citation:ocr-1", false),
            new ExtractedField(ReinsuranceFieldNames.Premium, "USD 999", 0.99, "llm-citation:ocr-2", false)
        ]);

        var merged = new ExtractionMerger().Merge(baseline, proposal);

        Assert.Equal("Alt Broker", merged.Fields.Single(field => field.Name == ReinsuranceFieldNames.Broker).Value);
        Assert.Equal("USD 100", merged.Fields.Single(field => field.Name == ReinsuranceFieldNames.Premium).Value);
    }

    private static ReinsuranceExtractionResult Baseline() => new(
        ReinsuranceDocumentType.StatementOfAccount,
        0.8,
        ReinsuranceFieldNames.Canonical.Select(name => new ExtractedField(name, name == ReinsuranceFieldNames.Premium ? "USD 100" : string.Empty, name == ReinsuranceFieldNames.Premium ? 0.9 : 0.12, name == ReinsuranceFieldNames.Premium ? "label:premium" : "missing", false)).ToList(),
        [],
        []);
}
