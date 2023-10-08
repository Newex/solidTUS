using System.Threading;
using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;

namespace SolidTUS.Tests.ProtocolHandlerTests.HEAD;

[UnitTest]
public class HeadRequestTests
{
    [Fact]
    public void Every_response_adds_CacheControl_header_to_no_store()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => HeadRequestHandler.SetResponseCacheControl(c)).GetValueOrDefault();
        var result = response?.ResponseHeaders[HeaderNames.CacheControl];

        // Assert
        Assert.Equal("no-store", result);
    }

    [Fact]
    public void When_the_file_exists_the_Upload_Offset_header_will_be_set_to_the_current_size_of_the_uploaded_file()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
        };
        file.AddBytes(212L);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => HeadRequestHandler.SetUploadOffsetHeader(c with
        {
            UploadFileInfo = file
        })).GetValueOrDefault();
        var result = response?.ResponseHeaders[TusHeaderNames.UploadOffset];

        // Assert
        Assert.Equal(212L.ToString(), result);
    }

    [Fact]
    public void When_the_file_exists_and_has_a_known_total_file_size_the_Upload_Length_header_will_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 4213L
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        })).GetValueOrDefault();
        var result = response?.ResponseHeaders[TusHeaderNames.UploadLength];

        // Assert
        Assert.Equal(4213L.ToString(), result);
    }

    [Fact]
    public void When_the_file_exists_and_has_a_known_total_file_size_the_Upload_Defer_Length_header_should_not_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 4213L
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        })).GetValueOrDefault();
        var result = response?.ResponseHeaders[TusHeaderNames.UploadDeferLength];

        // Assert
        Assert.Empty(result!);
    }

    [Fact]
    public void When_the_file_size_is_unknown_the_Upload_Defer_Length_header_will_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = null
        };
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = TusResult.Create(http.HttpContext.Request, http.HttpContext.Response);

        // Act
        var response = request.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        })).GetValueOrDefault();
        var result = response?.ResponseHeaders[TusHeaderNames.UploadDeferLength];

        // Assert
        Assert.Equal("1", result);
    }

    [Fact]
    public void When_upload_contains_metadata_the_Upload_Metadata_header_will_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            RawMetadata = "filename d29ybGRfZG9taW5hdGlvbl9wbGFuLnBkZg==,is_confidential"
        };
        var httpRequest = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var httpResponse = MockHttps.HttpResponse();
        var context = TusResult.Create(httpRequest, httpResponse);

        // Act
        var response = context.Map(c => HeadRequestHandler.SetMetadataHeader(c with
        {
            UploadFileInfo = file
        })).GetValueOrDefault();
        var result = response?.ResponseHeaders[TusHeaderNames.UploadMetadata];

        // Assert
        Assert.Equal(expected: file.RawMetadata, result);
    }
}
