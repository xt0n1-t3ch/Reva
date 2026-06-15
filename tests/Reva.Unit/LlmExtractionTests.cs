using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Parsing;

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

    [Fact]
    public async Task FakeChatClientProposalMergesNonMoneyAndKeepsDeterministicMoney()
    {
        var baseline = Baseline();
        var extractor = new OllamaLlmFieldExtractor(new FakeChatClient("""
            [
              { "name": "Broker", "value": "Alt Broker", "confidence": 0.91, "source": "llm-citation: broker label" },
              { "name": "Premium", "value": "USD 999", "confidence": 0.98, "source": "llm-citation: premium label" }
            ]
            """), Options.Create(new LlmExtractionOptions()));

        var proposal = await extractor.ProposeAsync(Parsed(), baseline, CancellationToken.None);
        var merged = new ExtractionMerger().Merge(baseline, proposal);

        Assert.NotNull(proposal);
        Assert.Equal("Alt Broker", merged.Fields.Single(field => field.Name == ReinsuranceFieldNames.Broker).Value);
        Assert.Equal("USD 100", merged.Fields.Single(field => field.Name == ReinsuranceFieldNames.Premium).Value);
    }

    [Fact]
    public async Task InvalidJsonProposalReturnsNullAndLeavesDeterministicUnchanged()
    {
        var baseline = Baseline();
        var extractor = new OllamaLlmFieldExtractor(new FakeChatClient("not-json", "also-not-json"), Options.Create(new LlmExtractionOptions()));

        var proposal = await extractor.ProposeAsync(Parsed(), baseline, CancellationToken.None);
        var merged = new ExtractionMerger().Merge(baseline, proposal);

        Assert.Null(proposal);
        Assert.Equal(baseline.Fields, merged.Fields);
    }

    private static ReinsuranceExtractionResult Baseline() => new(
        ReinsuranceDocumentType.StatementOfAccount,
        0.8,
        ReinsuranceFieldNames.Canonical.Select(name => new ExtractedField(name, name == ReinsuranceFieldNames.Premium ? "USD 100" : string.Empty, name == ReinsuranceFieldNames.Premium ? 0.9 : 0.12, name == ReinsuranceFieldNames.Premium ? "label:premium" : "missing", false)).ToList(),
        [],
        []);

    private static ParsedDocument Parsed() => new("test", "txt", "Broker: Alt Broker\nPremium: USD 100", "Broker: Alt Broker\nPremium: USD 100", string.Empty, [], []);

    private sealed class FakeChatClient(params string[] responses) : IChatClient
    {
        private int index;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var response = responses[Math.Min(index, responses.Length - 1)];
            index++;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
