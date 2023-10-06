using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;

using MSOptions = Microsoft.Extensions.Options.Options;

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
            ExpirationStrategy = ExpirationStrategy.Never,
            CreatedDate = new DateTimeOffset(2020, 06, 01, 12, 30, 00, TimeSpan.FromHours(0))
        };
        file.AddBytes(70);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType),
            (HeaderNames.ContentLength, "30"),
            (TusHeaderNames.UploadOffset, file.ByteOffset.ToString())
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);
        var handler = Setup.UploadFlow(file: file);

        // Act
        var process = await request.BindAsync(async c => await handler.PreUploadAsync(c, "file123", CancellationToken.None));
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
        };
        file.AddBytes(70);
        var uploadMetaHandler = MockHandlers.UploadMetaHandler(file);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType),
            (HeaderNames.ContentLength, "30"),
            (TusHeaderNames.UploadOffset, "XX") // <-- Bad header value
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);
        var handler = Setup.UploadFlow(uploadMetaHandler);

        // Act
        var process = await request.BindAsync(async c => await handler.PreUploadAsync(c, "file123", CancellationToken.None));
        var result = process.IsSuccess();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CreateWithUpload_should_call_OnUploadFinished_callback_if_whole_file_is_uploaded()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void PartialUpload_should_set_mode_to_Partial()
    {
        throw new NotImplementedException();
    }
}
