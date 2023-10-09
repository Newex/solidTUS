using SolidTUS.ProtocolHandlers.ProtocolExtensions;

namespace SolidTUS.Tests.ParserTests.RouteTemplateParserTests;

[UnitTest]
public class RouteTemplateParserTests
{
    [Theory]
    [InlineData("/api/route/{fileId}", "fileId", "/api/route/myFileA", "myFileA")]
    [InlineData("/api/route/{fileId:int}", "fileId", "/api/route/123", "123")]
    [InlineData("/api/{partialId:alpha}", "partialId", "/api/my-partial-id", "my-partial-id")]
    [InlineData("api/{partialId:alpha}", "partialId", "area/controller/api/my-partial-id", "my-partial-id")]
    [InlineData("api/some-route-prefix-{partialId:alpha}", "partialId", "area/controller/api/some-route-prefix-my-partial-id", "my-partial-id")]
    [InlineData("/api/route/{fileId}", "fileId", "/api/route", null)]
    public void Given_the_template_and_token_should_return_the_token_value_for_template(string template, string token, string input, string value)
    {
        // Act
        var result = ConcatenationRequestHandler.GetTemplateValue(input, template, token);

        // Assert
        result.Should().Be(value);
    }
}
