using System.Text.Json;
using Companion.Infrastructure.Services;
using Xunit;

namespace Companion.Tests;

public class AiJsonPayloadParserTests
{
    [Theory]
    [InlineData("""{"reply":"ok"}""", "ok")]
    [InlineData("```json\n{\"reply\":\"ok\"}\n```", "ok")]
    [InlineData("```\n{\"reply\":\"ok\"}\n```", "ok")]
    [InlineData("Before text\n{\"reply\":\"ok\"}", "ok")]
    [InlineData("{\"reply\":\"ok\"}\nAfter text", "ok")]
    [InlineData("Lead\n```json\n{\"reply\":\"ok\"}\n```\nTail", "ok")]
    public void ParseObjectDocument_ReturnsExpectedReply_ForSingleValidPayload(string input, string expectedReply)
    {
        using var document = AiJsonPayloadParser.ParseObjectDocument(input);

        Assert.NotNull(document);
        Assert.Equal(expectedReply, document!.RootElement.GetProperty("reply").GetString());
    }

    [Fact]
    public void ParseObjectDocument_ReturnsNull_ForInvalidJson()
    {
        using var document = AiJsonPayloadParser.ParseObjectDocument("```json\n{\"reply\":\n```");

        Assert.Null(document);
    }

    [Fact]
    public void ParseObjectDocument_PrefersMostLikelyValidObject_WhenMultipleObjectsExist()
    {
        const string input = """
        Noise before
        {"id":1}
        More noise
        ```json
        {"reply":"preferred","insights":[{"category":"Test","message":"Picked","priority":50}]}
        ```
        Trailing text
        {"reply":"smaller"}
        """;

        using var document = AiJsonPayloadParser.ParseObjectDocument(input);

        Assert.NotNull(document);
        Assert.Equal("preferred", document!.RootElement.GetProperty("reply").GetString());
        Assert.True(document.RootElement.TryGetProperty("insights", out _));
    }

    [Fact]
    public void ParseObjectDocument_IgnoresBracesInsideStrings()
    {
        const string input = """
        Before
        ```json
        {"reply":"value with { braces } inside string","meta":"still valid"}
        ```
        After
        """;

        using var document = AiJsonPayloadParser.ParseObjectDocument(input);

        Assert.NotNull(document);
        Assert.Equal(
            "value with { braces } inside string",
            document!.RootElement.GetProperty("reply").GetString());
    }
}
