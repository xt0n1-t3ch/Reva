using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;

namespace Reva.Infrastructure.Extraction;

public sealed record LlmFieldProposal(IReadOnlyList<ExtractedField> Fields);

public interface ILlmFieldExtractor
{
    Task<LlmFieldProposal?> ProposeAsync(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult, CancellationToken cancellationToken);
}

public sealed class DisabledLlmFieldExtractor : ILlmFieldExtractor
{
    public Task<LlmFieldProposal?> ProposeAsync(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult, CancellationToken cancellationToken) => Task.FromResult<LlmFieldProposal?>(null);
}

public sealed class OllamaLlmFieldExtractor : ILlmFieldExtractor
{
    public Task<LlmFieldProposal?> ProposeAsync(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult, CancellationToken cancellationToken) => Task.FromResult<LlmFieldProposal?>(null);
}

public interface IExtractionMerger
{
    ReinsuranceExtractionResult Merge(ReinsuranceExtractionResult deterministic, LlmFieldProposal? proposal);
}

public sealed class ExtractionMerger : IExtractionMerger
{
    public ReinsuranceExtractionResult Merge(ReinsuranceExtractionResult deterministic, LlmFieldProposal? proposal)
    {
        if (proposal is null || proposal.Fields.Count == 0)
        {
            return deterministic;
        }

        var fields = deterministic.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in proposal.Fields.Where(IsAcceptable))
        {
            if (IsMoney(candidate.Name) && fields.TryGetValue(candidate.Name, out var current) && !string.IsNullOrWhiteSpace(current.Value))
            {
                continue;
            }

            fields[candidate.Name] = candidate;
        }

        return deterministic with { Fields = ReinsuranceFieldNames.Canonical.Select(name => fields.TryGetValue(name, out var field) ? field : new ExtractedField(name, string.Empty, 0.12, "missing", false)).ToList() };
    }

    private static bool IsAcceptable(ExtractedField field) =>
        !string.IsNullOrWhiteSpace(field.Value)
        && field.Confidence >= 0.6
        && field.Source.Contains("citation", StringComparison.OrdinalIgnoreCase);

    private static bool IsMoney(string name) => name is ReinsuranceFieldNames.Premium or ReinsuranceFieldNames.Claims or ReinsuranceFieldNames.Commission or ReinsuranceFieldNames.Retention or ReinsuranceFieldNames.Limit;
}
