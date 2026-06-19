using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Core.Contracts;
using Reva.Core.Settings;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Export;
using Reva.Infrastructure.Knowledge;

namespace Reva.Unit;

public sealed class AgentChatServiceSettingsTests
{
    [Fact]
    public async Task StreamAsyncBuildsClientFromCurrentRuntimeSettings()
    {
        var factory = new CapturingFactory();
        var chat = new CapturingChatClient();
        factory.Client = chat;
        try
        {
            RuntimeSettings.Set(AppSettings.Default with
            {
                AiProvider = AiProviderNames.HuggingFace,
                AiBaseUrl = AiSettingsDefaults.HuggingFaceBaseUrl,
                AiApiKey = "hf-token",
                AiModel = "Qwen/Qwen2.5-VL-7B-Instruct"
            });
            var service = new AgentChatService(
                factory,
                Options.Create(new AgentChatOptions { MaxSteps = 3, NumCtx = 4096 }),
                new AppActionBus(),
                new ThrowingExporter(),
                new EmbeddedKnowledgeStore());

            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in service.StreamAsync([new ChatMessage(ChatRole.User, "hello")], [], CancellationToken.None))
            {
                updates.Add(update);
            }

            Assert.NotEmpty(updates);
            Assert.NotNull(factory.Request);
            Assert.Equal(AiProviderNames.HuggingFace, factory.Request.Provider);
            Assert.Equal(AiSettingsDefaults.HuggingFaceBaseUrl, factory.Request.BaseUrl);
            Assert.Equal("hf-token", factory.Request.ApiKey);
            Assert.Equal("Qwen/Qwen2.5-VL-7B-Instruct", factory.Request.Model);
            Assert.Equal(3, factory.Request.MaxSteps);
            Assert.Equal(4096, factory.Request.NumCtx);
            Assert.NotNull(chat.Options);
            Assert.Equal("Qwen/Qwen2.5-VL-7B-Instruct", chat.Options.ModelId);
            Assert.Equal(4096, chat.Options.AdditionalProperties?[AgentChatOptions.NumCtxPropertyName]);
            Assert.Equal("medium", chat.Options.AdditionalProperties?[AgentReasoningOptions.ReasoningEffortPropertyName]);
        }
        finally
        {
            RuntimeSettings.Set(AppSettings.Default);
        }
    }

    [Theory]
    [InlineData("OFF", null, false)]
    [InlineData("Low", "low", null)]
    [InlineData("Medium", "medium", null)]
    [InlineData("High", "high", null)]
    [InlineData("Max", "high", true)]
    [InlineData("bad-value", "medium", null)]
    public void ReasoningHeaderMapsToOpenAiCompatibleProperties(string header, string? expectedEffort, bool? expectedThinking)
    {
        var reasoning = AgentReasoningOptions.FromHeader(header);
        var properties = AgentReasoningMapper.BuildAdditionalProperties(
            2048,
            AiProviderNames.OpenAiCompatible,
            "Qwen3-32B",
            reasoning);

        Assert.Equal(2048, properties[AgentChatOptions.NumCtxPropertyName]);
        if (expectedEffort is null)
        {
            Assert.False(properties.ContainsKey(AgentReasoningOptions.ReasoningEffortPropertyName));
        }
        else
        {
            Assert.Equal(expectedEffort, properties[AgentReasoningOptions.ReasoningEffortPropertyName]);
        }

        if (expectedThinking is null)
        {
            Assert.False(properties.ContainsKey(AgentReasoningOptions.EnableThinkingPropertyName));
        }
        else
        {
            Assert.Equal(expectedThinking, properties[AgentReasoningOptions.EnableThinkingPropertyName]);
        }
    }

    private sealed class CapturingFactory : ILlmChatClientFactory
    {
        public IChatClient Client { get; set; } = new CapturingChatClient();
        public LlmChatClientRequest? Request { get; private set; }

        public IChatClient Create(LlmChatClientRequest request)
        {
            Request = request;
            return Client;
        }
    }

    private sealed class CapturingChatClient : IChatClient
    {
        public ChatOptions? Options { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Options = options;
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("ok")]);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingExporter : IDocumentExporter
    {
        public ExportPreview Preview(DocumentDetail document, ExportTemplate layout, int maxRows = 6) =>
            throw new NotSupportedException();

        public ExportFile Export(DocumentDetail document, ExportTemplate layout) =>
            throw new NotSupportedException();
    }
}
