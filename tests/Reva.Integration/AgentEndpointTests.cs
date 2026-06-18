using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.Integration;

public sealed class AgentEndpointTests(RevaWebApplicationFactory factory) : IClassFixture<RevaWebApplicationFactory>
{
    [Fact]
    public async Task AgentEndpointStreamsUiMessageProtocolFromStubbedAgent()
    {
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAgentChatService>();
                services.AddSingleton<IAgentChatService, StubAgentChatService>();
            });
        });
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var content = new StringContent("""
            {
              "id": "chat1",
              "messages": [
                {
                  "id": "user1",
                  "role": "user",
                  "parts": [{ "type": "text", "text": "List documents" }]
                }
              ]
            }
            """, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/agent", content);
        var body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("ui-message-stream-v1", response.Headers.GetValues("x-agui-protocol").Single());
        Assert.Equal("v1", response.Headers.GetValues("x-vercel-ai-ui-message-stream").Single());
        Assert.Contains("data: {\"type\":\"start\",\"messageId\":", body);
        Assert.Contains("data: {\"type\":\"text-start\",\"id\":", body);
        Assert.Contains("data: {\"type\":\"text-delta\",\"id\":", body);
        Assert.Contains("\"delta\":\"Stubbed answer\"", body);
        Assert.Contains("data: {\"type\":\"finish\"}\n\n", body);
        Assert.EndsWith("data: [DONE]\n\n", body);
    }

    private sealed class StubAgentChatService : IAgentChatService
    {
        public IReadOnlyList<AITool> BuildTools(IDocumentWorkflow workflow, RevaDbContext dbContext, IBdxReviewPayloadAssembler assembler, CancellationToken cancellationToken, IDataMaintenance? maintenance = null) => [];

        public async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<AITool> tools, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Assert.NotEmpty(messages);
            Assert.Empty(tools);
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Stubbed answer")]);
        }
    }
}
