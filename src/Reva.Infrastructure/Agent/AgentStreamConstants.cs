namespace Reva.Infrastructure.Agent;

public static class AgentStreamConstants
{
    public const string ContentType = "text/event-stream; charset=utf-8";
    public const string CacheControlHeader = "Cache-Control";
    public const string CacheControlValue = "no-cache";
    public const string ConnectionHeader = "Connection";
    public const string ConnectionValue = "keep-alive";
    public const string AguiProtocolHeader = "x-agui-protocol";
    public const string AguiProtocolValue = "ui-message-stream-v1";
    public const string VercelUiMessageStreamHeader = "x-vercel-ai-ui-message-stream";
    public const string VercelUiMessageStreamValue = "v1";
    public const string Done = "[DONE]";
    public const string DataPrefix = "data: ";
    public const string EventSuffix = "\n\n";
    public const string TypeProperty = "type";
    public const string Start = "start";
    public const string TextStart = "text-start";
    public const string TextDelta = "text-delta";
    public const string TextEnd = "text-end";
    public const string ToolInputAvailable = "tool-input-available";
    public const string ToolOutputAvailable = "tool-output-available";
    public const string Finish = "finish";
    public const string MessageId = "messageId";
    public const string Id = "id";
    public const string Delta = "delta";
    public const string ToolCallId = "toolCallId";
    public const string ToolName = "toolName";
    public const string Input = "input";
    public const string Output = "output";
    public const string UnavailableMessage = "The local model is unavailable. Deterministic features still work.";
}
