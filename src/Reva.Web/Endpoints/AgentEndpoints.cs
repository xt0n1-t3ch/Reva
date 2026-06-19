using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Knowledge;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;

namespace Reva.Web.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/agent", async (
            HttpContext httpContext,
            IAgentChatService agent,
            IDocumentWorkflow workflow,
            RevaDbContext dbContext,
            IBdxReviewPayloadAssembler assembler,
            IOptions<AgentChatOptions> options) =>
        {
            var cancellationToken = httpContext.RequestAborted;
            PrepareResponse(httpContext.Response);

            var parsed = await AgentChatRequestParser.ParseAsync(
                httpContext.Request.Body,
                options.Value.MaxMessages,
                options.Value.MaxRequestBytes,
                cancellationToken);
            if (!parsed.IsSuccess)
            {
                await WriteFramesAsync(
                    httpContext.Response,
                    AiSdkUiMessageStreamMapper.GracefulMessageAsync(parsed.ErrorMessage!, NewId(), NewId, cancellationToken),
                    cancellationToken);
                return;
            }

            var tools = agent.BuildTools(workflow, dbContext, assembler, cancellationToken);
            var reasoning = AgentReasoningOptions.FromHeader(httpContext.Request.Headers[AgentReasoningOptions.HeaderName].ToString());
            var updates = SafeUpdates(agent.StreamAsync(parsed.Messages, tools, cancellationToken, reasoning), cancellationToken);
            await WriteFramesAsync(
                httpContext.Response,
                AiSdkUiMessageStreamMapper.MapAsync(updates, NewId(), NewId, cancellationToken),
                cancellationToken);
        }).DisableAntiforgery().WithTags("Agent");

        return routes;
    }

    private static void PrepareResponse(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = AgentStreamConstants.ContentType;
        response.Headers[AgentStreamConstants.CacheControlHeader] = AgentStreamConstants.CacheControlValue;
        response.Headers[AgentStreamConstants.ConnectionHeader] = AgentStreamConstants.ConnectionValue;
        response.Headers[AgentStreamConstants.AguiProtocolHeader] = AgentStreamConstants.AguiProtocolValue;
        response.Headers[AgentStreamConstants.VercelUiMessageStreamHeader] = AgentStreamConstants.VercelUiMessageStreamValue;
    }

    private static async Task WriteFramesAsync(HttpResponse response, IAsyncEnumerable<string> frames, CancellationToken cancellationToken)
    {
        await foreach (var frame in frames.WithCancellation(cancellationToken))
        {
            await response.WriteAsync(frame, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> SafeUpdates(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = updates.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            ChatResponseUpdate? current = null;
            var failed = false;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                current = enumerator.Current;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed = true;
            }

            if (failed)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(AgentStreamConstants.UnavailableMessage)]);
                yield break;
            }

            yield return current!;
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N");
}
