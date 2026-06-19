using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
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

public sealed class OllamaLlmFieldExtractor(IChatClient chatClient, IOptions<LlmExtractionOptions> options) : ILlmFieldExtractor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<LlmFieldProposal?> ProposeAsync(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult, CancellationToken cancellationToken)
    {
        try
        {
            var first = await RequestAsync(BuildPrompt(documentContext, deterministicResult), cancellationToken);
            var parsed = ParseProposal(first);
            if (parsed is not null)
            {
                return parsed;
            }

            var retry = await RequestAsync(BuildRetryPrompt(documentContext, deterministicResult), cancellationToken);
            return ParseProposal(retry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"LLM field extraction failed: {ex.Message}");
            return null;
        }
    }

    private async Task<string> RequestAsync(string prompt, CancellationToken cancellationToken)
    {
        var response = await chatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, LlmExtractionOptions.SystemPrompt),
                new ChatMessage(ChatRole.User, prompt)
            ],
            new ChatOptions
            {
                ModelId = options.Value.Model,
                Temperature = 0
            },
            cancellationToken);
        return response.Text;
    }

    private static string BuildPrompt(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult) => $$"""
        Return strict JSON only. Return an array of objects with name, value, confidence, source.
        The name must be one of: {{string.Join(", ", ReinsuranceFieldNames.Canonical)}}.
        The source must contain the substring citation and name the supporting label, line, span, or table location.
        Use confidence from 0 to 1. Omit fields you cannot support from the document.
        Deterministic fields:
        {{JsonSerializer.Serialize(deterministicResult.Fields, SerializerOptions)}}
        Parsed markdown:
        {{Truncate(documentContext.Markdown, LlmExtractionOptions.MaxPromptCharacters)}}
        Parsed text:
        {{Truncate(documentContext.Text, LlmExtractionOptions.MaxPromptCharacters)}}
        """;

    private static string BuildRetryPrompt(ParsedDocument documentContext, ReinsuranceExtractionResult deterministicResult) => $$"""
        JSON array only. Shape: [{"name":"Broker","value":"Example","confidence":0.8,"source":"llm-citation: line text"}]
        Allowed names: {{string.Join(", ", ReinsuranceFieldNames.Canonical)}}.
        Context:
        {{Truncate(documentContext.Text, LlmExtractionOptions.RetryPromptCharacters)}}
        Existing:
        {{JsonSerializer.Serialize(deterministicResult.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)), SerializerOptions)}}
        """;

    private static LlmFieldProposal? ParseProposal(string content)
    {
        var json = ExtractJson(content);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var proposals = JsonSerializer.Deserialize<IReadOnlyList<LlmFieldProposalItem>>(json, SerializerOptions);
            if (proposals is null)
            {
                return null;
            }

            var fields = proposals
                .Where(IsValid)
                .Select(item => new ExtractedField(item.Name.Trim(), item.Value.Trim(), Math.Clamp(item.Confidence, 0, 1), item.Source.Trim(), false))
                .ToList();
            return fields.Count == 0 ? null : new LlmFieldProposal(fields);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsValid(LlmFieldProposalItem item) =>
        !string.IsNullOrWhiteSpace(item.Name)
        && !string.IsNullOrWhiteSpace(item.Value)
        && !string.IsNullOrWhiteSpace(item.Source)
        && item.Confidence >= 0
        && item.Confidence <= 1
        && ReinsuranceFieldNames.Canonical.Contains(item.Name, StringComparer.Ordinal);

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        var fenceStart = trimmed.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var afterFence = trimmed[(fenceStart + 3)..].TrimStart();
            if (afterFence.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                afterFence = afterFence[4..].TrimStart();
            }

            var fenceEnd = afterFence.IndexOf("```", StringComparison.Ordinal);
            trimmed = fenceEnd >= 0 ? afterFence[..fenceEnd].Trim() : afterFence.Trim();
        }

        var start = trimmed.IndexOf('[', StringComparison.Ordinal);
        var end = trimmed.LastIndexOf(']');
        return start >= 0 && end >= start ? trimmed[start..(end + 1)] : string.Empty;
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record LlmFieldProposalItem(string Name, string Value, double Confidence, string Source);
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
