using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Reva.Infrastructure.Agent;

namespace Reva.Unit;

public sealed class AgentStreamTests
{
    [Fact]
    public async Task MapperEmitsUiMessageStreamFramesInOrder()
    {
        var frames = new List<string>();
        await foreach (var frame in AiSdkUiMessageStreamMapper.MapAsync(ScriptedUpdates(), "msg1", FixedIds(), CancellationToken.None))
        {
            frames.Add(frame);
        }

        Assert.Equal([
            "data: {\"type\":\"start\",\"messageId\":\"msg1\"}\n\n",
            "data: {\"type\":\"text-start\",\"id\":\"text1\"}\n\n",
            "data: {\"type\":\"text-delta\",\"id\":\"text1\",\"delta\":\"Hello \"}\n\n",
            "data: {\"type\":\"text-delta\",\"id\":\"text1\",\"delta\":\"Tony\"}\n\n",
            "data: {\"type\":\"tool-input-available\",\"toolCallId\":\"call1\",\"toolName\":\"get_document\",\"input\":{\"documentId\":\"doc1\"}}\n\n",
            "data: {\"type\":\"tool-output-available\",\"toolCallId\":\"call1\",\"output\":{\"id\":\"doc1\",\"status\":\"Extracted\"}}\n\n",
            "data: {\"type\":\"text-end\",\"id\":\"text1\"}\n\n",
            "data: {\"type\":\"finish\"}\n\n",
            "data: [DONE]\n\n"
        ], frames);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ScriptedUpdates([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Hello ")]);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Tony")]);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call1", "get_document", new Dictionary<string, object?> { ["documentId"] = "doc1" })]);
        yield return new ChatResponseUpdate(ChatRole.Tool, [new FunctionResultContent("call1", new { id = "doc1", status = "Extracted" })]);
    }

    private static Func<string> FixedIds()
    {
        var ids = new Queue<string>(["text1"]);
        return () => ids.Dequeue();
    }
}
