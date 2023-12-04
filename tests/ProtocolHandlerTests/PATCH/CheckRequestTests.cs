using CSharpFunctionalExtensions;
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
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, "wrong_content_type")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(PatchRequestHandler.CheckContentType);
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(415);
    }

    [Fact]
    public void Proper_content_type_returns_success()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentType, TusHeaderValues.PatchContentType)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(PatchRequestHandler.CheckContentType);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Request_without_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            ("No-Upload-Offset", "Missing")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Request_with_wrong_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "Missing")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Proper_Upload_Offset_header_returns_success()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "300")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(PatchRequestHandler.CheckUploadOffset);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Request_with_mismatching_byte_offset_returns_409_status_code()
    {
        // Arrange
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
        };
        fileInfo.AddBytes(20);
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "30")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckConsistentByteOffset(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(409);
    }

    [Fact]
    public void Request_with_matching_byte_offset_returns_success()
    {
        // Arrange
        var fileInfo = new UploadFileInfo();
        fileInfo.AddBytes(100);
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "100")
        );
        var response = MockHttps.HttpResponse();
        var context = TusResult.Create(request, response);

        // Act
        var check = context.Bind(c => PatchRequestHandler.CheckConsistentByteOffset(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = check.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Upload_which_is_bigger_than_given_FileSize_returns_400_status_code()
    {
        // Arrange upload 200
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (HeaderNames.ContentLength, "200"),
            (TusHeaderNames.UploadOffset, "30")
        );
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            FileSize = 200
        };
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckUploadExceedsFileSize(c with
        {
            UploadFileInfo = fileInfo
        }));
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Upload_which_is_less_than_given_FileSize_returns_success()
    {
        // Arrange upload 200
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, "30"),
            (HeaderNames.ContentLength, "100") // <- 100 + 30 < 200
        );
        var fileInfo = RandomEntities.UploadFileInfo() with
        {
            FileSize = 200
        };
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckUploadExceedsFileSize(c with
        {
            UploadFileInfo = fileInfo,
            FileSize = fileInfo.FileSize
        }));
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Request_with_negative_Upload_Offset_header_returns_400_status_code()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, (-20L).ToString())
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => PatchRequestHandler.CheckUploadOffset(c));
        var result = response.StatusCode(204);

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Request_with_positive_Upload_Offset_header_returns_success()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, 20L.ToString())
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(PatchRequestHandler.CheckUploadOffset);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Request_with_zero_Upload_Offset_header_returns_success()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadOffset, 0L.ToString())
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(PatchRequestHandler.CheckUploadOffset);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void No_errors_during_setting_file_size_returns_success()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = null
        };
        var uploadMetaHandler = MockHandlers.UploadMetaHandler(file, updated: true);
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadLength, 200L.ToString())
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());
        var handler = new PatchRequestHandler();

        // Act
        var response = context.Bind(PatchRequestHandler.CheckUploadLength);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }
}
