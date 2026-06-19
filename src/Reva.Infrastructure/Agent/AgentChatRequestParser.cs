using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Reva.Core.Settings;

namespace Reva.Infrastructure.Agent;

public sealed record AgentChatParseResult(IReadOnlyList<ChatMessage> Messages, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
}

public static class AgentChatRequestParser
{
    private const string MessagesProperty = "messages";
    private const string RoleProperty = "role";
    private const string PartsProperty = "parts";
    private const string PartTypeText = "text";
    private const string PartTypeFile = "file";
    private const string TextProperty = "text";
    private const string MediaTypeProperty = "mediaType";
    private const string UrlProperty = "url";
    private const string DataUrlPrefix = "data:";
    private const string Base64Marker = ";base64";
    private const string ImageMediaPrefix = "image/";
    private const string OctetStreamMediaType = "application/octet-stream";

    public static async Task<AgentChatParseResult> ParseAsync(Stream body, int maxMessages, int maxBytes, CancellationToken cancellationToken)
    {
        var bytes = await ReadBoundedAsync(body, maxBytes, cancellationToken);
        if (bytes is null)
        {
            return new AgentChatParseResult(BuildSystemOnly(), $"The chat request is too large. Send at most {maxBytes} bytes.");
        }

        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!document.RootElement.TryGetProperty(MessagesProperty, out var messagesElement) || messagesElement.ValueKind is not JsonValueKind.Array)
            {
                return new AgentChatParseResult(BuildSystemOnly(), "The chat request did not include a valid messages array.");
            }

            if (messagesElement.GetArrayLength() > maxMessages)
            {
                return new AgentChatParseResult(BuildSystemOnly(), $"The chat request has too many messages. Send at most {maxMessages} messages.");
            }

            var messages = BuildSystemOnly();
            foreach (var messageElement in messagesElement.EnumerateArray())
            {
                if (!TryReadRole(messageElement, out var role) || !messageElement.TryGetProperty(PartsProperty, out var partsElement) || partsElement.ValueKind is not JsonValueKind.Array)
                {
                    continue;
                }

                var contents = new List<AIContent>();
                foreach (var partElement in partsElement.EnumerateArray())
                {
                    AddPart(contents, partElement);
                }

                if (contents.Count > 0)
                {
                    messages.Add(new ChatMessage(role, contents));
                }
            }

            return new AgentChatParseResult(messages, null);
        }
        catch (JsonException)
        {
            return new AgentChatParseResult(BuildSystemOnly(), "The chat request was not valid JSON.");
        }
    }

    public static AgentChatParseResult ParseJson(string json, int maxMessages = AgentChatOptions.DefaultMaxMessages, int maxBytes = AgentChatOptions.DefaultMaxRequestBytes)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return ParseAsync(stream, maxMessages, maxBytes, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static async Task<byte[]?> ReadBoundedAsync(Stream body, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            await using var output = new MemoryStream();
            while (true)
            {
                var read = await body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    return output.ToArray();
                }

                if (output.Length + read > maxBytes)
                {
                    return null;
                }

                output.Write(buffer, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static List<ChatMessage> BuildSystemOnly() =>
    [
        new(ChatRole.System, SystemPrompt)
    ];

    private static string SystemPrompt => string.Join(" ",
        $"You are the assistant inside {AppSettings.Default.ProductName}, a reinsurance bordereaux (BDX) ingestion and reconciliation console.",
        "Operators ask about uploaded documents: extracted fields, classification, exceptions, reconciliation of stated control totals against line items, and source citations.",
        "Always ground answers in tool results. The deterministic .NET engine is the source of truth — never invent figures, monetary amounts, or document ids.",
        "When you reference a value, name the document and field it came from. If a tool returns nothing, say so plainly instead of guessing.",
        "For product, methodology, and reinsurance industry questions, call search_knowledge and answer only from the returned seeded knowledge snippets.",
        "When the user attaches an image, read it directly and answer about its contents — do not call document tools for an attached image unless the user asks you to ingest or reconcile it.",
        "Be concise and precise. Prefer short, scannable answers for an expert audience.");

    private static bool TryReadRole(JsonElement messageElement, out ChatRole role)
    {
        role = ChatRole.User;
        if (!messageElement.TryGetProperty(RoleProperty, out var roleElement) || roleElement.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        role = roleElement.GetString() switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            _ => ChatRole.User
        };
        return true;
    }

    private static void AddPart(List<AIContent> contents, JsonElement partElement)
    {
        if (!partElement.TryGetProperty(AgentStreamConstants.TypeProperty, out var typeElement) || typeElement.ValueKind is not JsonValueKind.String)
        {
            return;
        }

        switch (typeElement.GetString())
        {
            case PartTypeText:
                AddText(contents, partElement);
                break;
            case PartTypeFile:
                AddFile(contents, partElement);
                break;
        }
    }

    private static void AddText(List<AIContent> contents, JsonElement partElement)
    {
        if (partElement.TryGetProperty(TextProperty, out var textElement) && textElement.ValueKind is JsonValueKind.String)
        {
            var text = textElement.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                contents.Add(new TextContent(text));
            }
        }
    }

    private static void AddFile(List<AIContent> contents, JsonElement partElement)
    {
        if (!partElement.TryGetProperty(MediaTypeProperty, out var mediaTypeElement) || mediaTypeElement.ValueKind is not JsonValueKind.String
            || !partElement.TryGetProperty(UrlProperty, out var urlElement) || urlElement.ValueKind is not JsonValueKind.String)
        {
            return;
        }

        var mediaType = mediaTypeElement.GetString() ?? string.Empty;
        var url = urlElement.GetString() ?? string.Empty;
        if (!mediaType.StartsWith(ImageMediaPrefix, StringComparison.OrdinalIgnoreCase) || !TryParseDataUrl(url, mediaType, out var bytes, out var parsedMediaType))
        {
            return;
        }

        contents.Add(new DataContent(bytes, parsedMediaType));
    }

    private static bool TryParseDataUrl(string url, string fallbackMediaType, out byte[] bytes, out string mediaType)
    {
        bytes = [];
        mediaType = fallbackMediaType;
        if (!url.StartsWith(DataUrlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = url.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex <= DataUrlPrefix.Length)
        {
            return false;
        }

        var metadata = url[DataUrlPrefix.Length..commaIndex];
        if (!metadata.Contains(Base64Marker, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var markerIndex = metadata.IndexOf(';', StringComparison.Ordinal);
        mediaType = markerIndex > 0 ? metadata[..markerIndex] : fallbackMediaType;
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            mediaType = OctetStreamMediaType;
        }

        try
        {
            bytes = Convert.FromBase64String(url[(commaIndex + 1)..]);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
