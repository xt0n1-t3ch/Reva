using Microsoft.Extensions.AI;
using Reva.Infrastructure.Agent;

namespace Reva.Unit;

public sealed class AgentRequestParserTests
{
    [Fact]
    public void ParserConvertsTextAndImagePartsToChatMessages()
    {
        var imageBytes = Convert.ToBase64String([1, 2, 3, 4]);
        var parsed = AgentChatRequestParser.ParseJson($$"""
            {
              "id": "chat1",
              "messages": [
                {
                  "id": "user1",
                  "role": "user",
                  "parts": [
                    { "type": "text", "text": "Read this image" },
                    { "type": "file", "mediaType": "image/png", "url": "data:image/png;base64,{{imageBytes}}", "filename": "page.png" },
                    { "type": "unknown", "value": true }
                  ]
                }
              ],
              "trigger": "submit-message"
            }
            """);

        Assert.True(parsed.IsSuccess);
        Assert.Equal(2, parsed.Messages.Count);
        Assert.Equal(ChatRole.System, parsed.Messages[0].Role);
        Assert.Equal(ChatRole.User, parsed.Messages[1].Role);
        Assert.IsType<TextContent>(parsed.Messages[1].Contents[0]);
        var image = Assert.IsType<DataContent>(parsed.Messages[1].Contents[1]);
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, image.Data.ToArray());
    }
}
