using System.Threading;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

/// <summary>
/// These tests are not exactly unit tests - but tests a whole pipeline workflow
/// </summary>
[Feature("UploadFlow")]
public class UploadRequestValidationTests
{
    [Fact]
    public async void Valid_request_returns_success()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 100,
            ByteOffset = 70
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType),
            (HeaderNames.ContentLength, "30"),
            (TusHeaderNames.UploadOffset, file.ByteOffset.ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = Setup.UploadFlow(file: file);

        // Act
        var process = await request.BindAsync(async c => await handler.StartUploadingAsync(c, c.FileID));
        var result = process.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async void Invalid_request_returns_error()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 100,
            ByteOffset = 70
        };
        var uploadMetaHandler = MockHandlers.UploadMetaHandler(file);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType),
            (HeaderNames.ContentLength, "30"),
            (TusHeaderNames.UploadOffset, "XX") // <-- Bad header value
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = Setup.UploadFlow(uploadMetaHandler);

        // Act
        var process = await request.BindAsync(async c => await handler.StartUploadingAsync(c, c.FileID));
        var result = process.IsSuccess();

        // Assert
        Assert.False(result);
    }
}
