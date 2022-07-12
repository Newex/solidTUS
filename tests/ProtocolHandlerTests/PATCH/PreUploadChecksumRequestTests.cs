using System;
using System.Threading;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Parsers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;

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
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Map(c => ChecksumRequestHandler.ParseChecksum(c));
        var result = response.IsRight;

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
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Map(c => ChecksumRequestHandler.ParseChecksum(c));
        var result = response.Match(
            r => r.IsSome,
            _ => throw new NotImplementedException()
        );

        // Assert
        Assert.False(result);
    }
}
