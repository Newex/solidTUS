using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    [Fact]
    public async void CreateWithUpload_should_call_OnUploadFinished_callback_if_whole_file_is_uploaded()
    {
        // Arrange
        var contentString = "Hello World!";
        var fileSize = Encoding.UTF8.GetByteCount(contentString);
        var array = Encoding.UTF8.GetBytes(contentString);
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = fileSize,
            ByteOffset = 0
        };
        var http = MockHttps.HttpRequest("POST",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType),
            (HeaderNames.ContentLength, "100"),
            (TusHeaderNames.UploadOffset, file.ByteOffset.ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        using var memory = new MemoryStream(array);
        var reader = PipeReader.Create(memory);
        var handler = Setup.TusCreationContext(withUpload: true, reader, bytesWritten: fileSize, fileInfo: file);
        var called = false;
        Task OnUploadFinished()
        {
            called = true;
            return Task.CompletedTask;
        }

        // Act: MUST call the on upload finished before starting upload!
        handler.OnUploadFinished(OnUploadFinished);
        await handler.StartCreationAsync("file_123", "/upload-to-here");

        // Assert
        Assert.True(called);
    }
}
