using System;
using System.Threading;
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
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadChecksum, $"sha1 {cipher}")
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => ChecksumRequestHandler.ParseChecksum(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Invalid_base64_encoded_checksum_does_not_parse()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadChecksum, "sha1 nonBase64EncodedChecksum-")
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => ChecksumRequestHandler.ParseChecksum(c));
        var result = response.IsSuccess();

        // Assert
        Assert.False(result);
    }
}
