using System.Threading;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;

namespace SolidTUS.Tests.ProtocolHandlerTests.PATCH;

[UnitTest]
public class CheckRequestTests
{
    [Fact]
    public void Incorrect_or_missing_content_type_returns_415_status_code()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, "wrong_content_type")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckContentType(c));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 415, result);
    }

    [Fact]
    public void Proper_content_type_returns_success()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType)
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckContentType(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Request_without_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            ("No-Upload-Offset", "Missing")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Request_with_wrong_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "Missing")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Proper_Upload_Offset_header_returns_success()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "300")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Request_with_mismatching_byte_offset_returns_409_status_code()
    {
        // Arrange
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            ByteOffset = 20
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "30")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckConsistentByteOffset(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 409, result);
    }

    [Fact]
    public void Request_with_matching_byte_offset_returns_success()
    {
        // Arrange
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            ByteOffset = 100
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "100")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckConsistentByteOffset(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Upload_which_is_bigger_than_given_FileSize_returns_400_status_code()
    {
        // Arrange upload 200
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentLength, "200"),
            (TusHeaderNames.UploadOffset, "30")
        );
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            FileSize = 200
        };
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadExceedsFileSize(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Upload_which_is_less_than_given_FileSize_returns_success()
    {
        // Arrange upload 200
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "30"),
            (HeaderNames.ContentLength, "100") // <- 100 + 30 < 200
        );
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            FileSize = 200
        };
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadExceedsFileSize(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Request_with_negative_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, (-20L).ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Request_with_positive_Upload_Offset_header_returns_success()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, 20L.ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async void Error_during_setting_file_size_returns_500_status_code()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = null
        };
        var uploadStorageHandler = MockHandlers.UploadStorageHandler(file, setFileSize: false);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadLength, 200L.ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = new PatchRequestHandler(uploadStorageHandler);

        // Act
        var response = await request.BindAsync(async c => await handler.CheckUploadLengthAsync(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 500, result);
    }

    [Fact]
    public async void No_errors_during_setting_file_size_returns_success()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = null
        };
        var uploadStorageHandler = MockHandlers.UploadStorageHandler(file, setFileSize: true);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadLength, 200L.ToString())
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = new PatchRequestHandler(uploadStorageHandler);

        // Act
        var response = await request.BindAsync(async c => await handler.CheckUploadLengthAsync(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }
}
