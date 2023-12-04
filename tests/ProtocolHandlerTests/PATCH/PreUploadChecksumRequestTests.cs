using CSharpFunctionalExtensions;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Parsers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;

namespace SolidTUS.Tests.ProtocolHandlerTests.PATCH;

[UnitTest]
public class PreUploadChecksumRequestTests
{
    [Fact]
    public void Valid_base64_encoded_checksum_parses_successfully()
    {
        // Arrange
        var cipher = Base64Converters.Encode("checksumOfUploadChunk");
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadChecksum, $"sha1 {cipher}")
        );
        var response = MockHttps.HttpResponse();
        var context = TusResult.Create(request, response);

        // Act
        var checksum = context.Bind(ChecksumRequestHandler.ParseChecksum);
        var result = checksum.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Invalid_base64_encoded_checksum_does_not_parse()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadChecksum, "sha1 nonBase64EncodedChecksum-")
        );
        var response = MockHttps.HttpResponse();
        var context = TusResult.Create(request, response);

        // Act
        var checksum = context.Bind(ChecksumRequestHandler.ParseChecksum);
        var result = checksum.IsSuccess();

        // Assert
        Assert.False(result);
    }
}
