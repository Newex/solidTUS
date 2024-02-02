using Microsoft.Net.Http.Headers;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.Models.Functional;
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
        var context = TusResult.Create(http, MockHttps.HttpResponse());

        // Act
        var (response, _) = context.Map(HeadRequestHandler.SetResponseCacheControl);
        var result = response?.ResponseHeaders[HeaderNames.CacheControl];

        // Assert
        Assert.Equal("no-store", result);
    }

    [Fact]
    public void When_the_file_exists_the_Upload_Offset_header_will_be_set_to_the_current_size_of_the_uploaded_file()
    {
        // Arrange
        var file = new UploadFileInfo();
        file.AddBytes(212L);
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(http, MockHttps.HttpResponse());

        // Act
        var (response, _) = context.Map(c => HeadRequestHandler.SetUploadOffsetHeader(c with
        {
            UploadFileInfo = file
        }));
        var result = response?.ResponseHeaders[TusHeaderNames.UploadOffset];

        // Assert
        result.ToString().Should().Be("212");
    }

    [Fact]
    public void When_the_file_exists_and_has_a_known_total_file_size_the_Upload_Length_header_will_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 4213L
        };
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var (response, _) = context.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        }));
        var result = response?.ResponseHeaders[TusHeaderNames.UploadLength];

        // Assert
        result.ToString().Should().Be("4213");
    }

    [Fact]
    public void When_the_file_exists_and_has_a_known_total_file_size_the_Upload_Defer_Length_header_should_not_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = 4213L
        };
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var (response, _) = context.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        }));
        var result = response?.ResponseHeaders[TusHeaderNames.UploadDeferLength];

        // Assert
        result.ToString().Should().BeNullOrEmpty();
    }

    [Fact]
    public void When_the_file_size_is_unknown_the_Upload_Defer_Length_header_will_be_set()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo() with
        {
            FileSize = null
        };
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var (response, _) = context.Map(c => HeadRequestHandler.SetUploadLengthOrDeferred(c with
        {
            UploadFileInfo = file
        }));
        var result = response?.ResponseHeaders[TusHeaderNames.UploadDeferLength];

        // Assert
        result.ToString().Should().Be("1");
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
        var (response, _) = context.Map(c => HeadRequestHandler.SetMetadataHeader(c with
        {
            UploadFileInfo = file
        }));
        var result = response?.ResponseHeaders[TusHeaderNames.UploadMetadata];

        // Assert
        Assert.Equal(expected: file.RawMetadata, result);
    }
}
