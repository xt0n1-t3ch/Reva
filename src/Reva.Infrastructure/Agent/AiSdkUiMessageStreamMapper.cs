using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;

namespace Reva.Infrastructure.Agent;

public static class AiSdkUiMessageStreamMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async IAsyncEnumerable<string> MapAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        string messageId,
        Func<string> idFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var textId = idFactory();
        var textStarted = false;
        yield return Frame(StartJson(messageId));

        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextContent text when !string.IsNullOrEmpty(text.Text):
                        if (!textStarted)
                        {
                            textStarted = true;
                            yield return Frame(TextStartJson(textId));
                        }

                        yield return Frame(TextDeltaJson(textId, text.Text));
                        break;
                    case FunctionCallContent call:
                        yield return Frame(ToolInputJson(call.CallId, call.Name, ToJsonElement(call.Arguments, emptyObject: true)));
                        break;
                    case FunctionResultContent result:
                        yield return Frame(ToolOutputJson(result.CallId, ToJsonElement(result.Result, emptyObject: false)));
                        break;
                }
            }
        }

        if (textStarted)
        {
            yield return Frame(TextEndJson(textId));
        }

        yield return Frame(FinishJson());
        yield return Frame(AgentStreamConstants.Done);
    }

    public static async IAsyncEnumerable<string> GracefulMessageAsync(
        string message,
        string messageId,
        Func<string> idFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var update = new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(message)]);
        await foreach (var frame in MapAsync(Single(update), messageId, idFactory, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return frame;
        }
    }

    public static string Frame(string payload) => AgentStreamConstants.DataPrefix + payload + AgentStreamConstants.EventSuffix;

    private static async IAsyncEnumerable<ChatResponseUpdate> Single(ChatResponseUpdate update)
    {
        await Task.CompletedTask;
        yield return update;
    }

    private static string StartJson(string messageId) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.Start);
        writer.WriteString(AgentStreamConstants.MessageId, messageId);
    });

    private static string TextStartJson(string id) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.TextStart);
        writer.WriteString(AgentStreamConstants.Id, id);
    });

    private static string TextDeltaJson(string id, string delta) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.TextDelta);
        writer.WriteString(AgentStreamConstants.Id, id);
        writer.WriteString(AgentStreamConstants.Delta, delta);
    });

    private static string TextEndJson(string id) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.TextEnd);
        writer.WriteString(AgentStreamConstants.Id, id);
    });

    private static string ToolInputJson(string callId, string name, JsonElement input) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.ToolInputAvailable);
        writer.WriteString(AgentStreamConstants.ToolCallId, callId);
        writer.WriteString(AgentStreamConstants.ToolName, name);
        writer.WritePropertyName(AgentStreamConstants.Input);
        input.WriteTo(writer);
    });

    private static string ToolOutputJson(string callId, JsonElement output) => FixedJson(writer =>
    {
        writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.ToolOutputAvailable);
        writer.WriteString(AgentStreamConstants.ToolCallId, callId);
        writer.WritePropertyName(AgentStreamConstants.Output);
        output.WriteTo(writer);
    });

    private static string FinishJson() => FixedJson(writer => writer.WriteString(AgentStreamConstants.TypeProperty, AgentStreamConstants.Finish));

    private static string FixedJson(Action<Utf8JsonWriter> writeProperties)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writeProperties(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonElement ToJsonElement(object? value, bool emptyObject)
    {
        if (value is null)
        {
            return JsonDocument.Parse(emptyObject ? "{}" : "null").RootElement.Clone();
        }

        if (value is JsonElement element)
        {
            return element.Clone();
        }

        return JsonSerializer.SerializeToElement(value, SerializerOptions).Clone();
    }
}
