using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Parsing;

namespace Reva.Ai;

public sealed class VlmFieldExtractor(
    IChatClient chatClient,
    IOptions<AiProcessingOptions> options) : ILlmFieldExtractor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<LlmFieldProposal?> ProposeAsync(
        ParsedDocument documentContext,
        ReinsuranceExtractionResult deterministicResult,
        CancellationToken cancellationToken)
    {
        if (documentContext is null || deterministicResult is null)
        {
            return null;
        }

        var settings = options.Value;
        var timeoutSeconds = settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : AiProcessingOptions.DefaultTimeoutSeconds;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var images = await CollectImagesAsync(documentContext, timeout.Token);
            var contents = BuildContents(images, documentContext, deterministicResult);
            if (contents.Count == 0)
            {
                return null;
            }

            var response = await chatClient.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, VlmExtractionDefaults.SystemPrompt),
                    new ChatMessage(ChatRole.User, contents)
                ],
                new ChatOptions
                {
                    ModelId = ResolveModel(settings),
                    Temperature = 0
                },
                timeout.Token);

            return ParseProposal(response.Text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ResolveModel(AiProcessingOptions settings) =>
        string.IsNullOrWhiteSpace(settings.ActiveModel) ? AiProcessingOptions.DefaultActiveModel : settings.ActiveModel.Trim();

    private static async Task<IReadOnlyList<byte[]>> CollectImagesAsync(ParsedDocument documentContext, CancellationToken cancellationToken)
    {
        var images = new List<byte[]>(VlmExtractionDefaults.MaxPages);
        foreach (var page in documentContext.Pages.OrderBy(page => page.Page).Take(VlmExtractionDefaults.MaxPages))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = await ReadImageAsync(page.ImagePath, cancellationToken);
            if (bytes is not null)
            {
                images.Add(bytes);
            }
        }

        return images;
    }

    private static async Task<byte[]?> ReadImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            return bytes.Length == 0 ? null : bytes;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static List<AIContent> BuildContents(
        IReadOnlyList<byte[]> images,
        ParsedDocument documentContext,
        ReinsuranceExtractionResult deterministicResult)
    {
        var textOnly = images.Count == 0;
        if (textOnly && !HasUsableText(documentContext))
        {
            return [];
        }

        var contents = new List<AIContent> { new TextContent(BuildPrompt(deterministicResult, textOnly, documentContext)) };
        foreach (var image in images)
        {
            contents.Add(new DataContent(image, VlmExtractionDefaults.PngMediaType));
        }

        return contents;
    }

    private static bool HasUsableText(ParsedDocument documentContext) =>
        !string.IsNullOrWhiteSpace(documentContext.Markdown) || !string.IsNullOrWhiteSpace(documentContext.Text);

    private static string BuildPrompt(ReinsuranceExtractionResult deterministicResult, bool textOnly, ParsedDocument documentContext)
    {
        var deterministic = JsonSerializer.Serialize(
            deterministicResult.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)),
            SerializerOptions);

        var body = $$"""
            Return strict JSON only: an array of objects with name, value, confidence, source.
            The name must be exactly one of: {{string.Join(", ", ReinsuranceFieldNames.Canonical)}}.
            The source must contain the substring "{{VlmExtractionDefaults.CitationToken}}" and quote the page label, line, or table cell that supports the value.
            Use confidence from 0 to 1. Omit any field you cannot support from the document.
            Example: [{"name":"Broker","value":"Acme Re","confidence":0.9,"source":"{{VlmExtractionDefaults.CitationToken}}: header line 'Broker: Acme Re'"}]
            Deterministic fields already known:
            {{deterministic}}
            """;

        if (!textOnly)
        {
            return body;
        }

        var text = Truncate(string.IsNullOrWhiteSpace(documentContext.Markdown) ? documentContext.Text : documentContext.Markdown);
        return $$"""
            {{body}}
            No page images are available. Use this parsed document text:
            {{text}}
            """;
    }

    private static LlmFieldProposal? ParseProposal(string? content)
    {
        var json = ExtractJson(content);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<IReadOnlyList<VlmProposalItem>>(json, SerializerOptions);
            if (items is null)
            {
                return null;
            }

            var fields = items
                .Where(IsValid)
                .Select(item => new ExtractedField(item.Name.Trim(), item.Value.Trim(), Math.Clamp(item.Confidence, 0, 1), NormalizeSource(item.Source), false))
                .ToList();
            return fields.Count == 0 ? null : new LlmFieldProposal(fields);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeSource(string source)
    {
        var trimmed = source.Trim();
        return trimmed.Contains("citation", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{VlmExtractionDefaults.CitationToken}: {trimmed}";
    }

    private static bool IsValid(VlmProposalItem item) =>
        !string.IsNullOrWhiteSpace(item.Name)
        && !string.IsNullOrWhiteSpace(item.Value)
        && !string.IsNullOrWhiteSpace(item.Source)
        && item.Confidence >= 0
        && item.Confidence <= 1
        && ReinsuranceFieldNames.Canonical.Contains(item.Name.Trim(), StringComparer.Ordinal);

    private static string ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

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

    private static string Truncate(string value) =>
        string.IsNullOrEmpty(value) || value.Length <= VlmExtractionDefaults.MaxTextCharacters ? value : value[..VlmExtractionDefaults.MaxTextCharacters];

    private sealed record VlmProposalItem(string Name, string Value, double Confidence, string Source);
}
