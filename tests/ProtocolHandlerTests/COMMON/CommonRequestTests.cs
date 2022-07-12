using System.Threading;
using SolidTUS.Constants;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;

namespace SolidTUS.Tests.ProtocolHandlerTests.COMMON;

[UnitTest]
public class CommonRequestTests
{
    [Fact]
    public void Supported_TUS_version_returns_success()
    {
        // Arrange
        var http = MockHttps.HttpRequest("GET",
            (TusHeaderNames.Resumable, "1.0.0"));
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => CommonRequestHandler.CheckTusVersion(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Unsupported_TUS_version_returns_412_error()
    {
        // Arrange
        var http = MockHttps.HttpRequest("SOME-HTTP-METHOD",
            (TusHeaderNames.Resumable, "0.2.2"));
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => CommonRequestHandler.CheckTusVersion(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 412, result);
    }

    [Fact]
    public void Unsupported_TUS_version_is_ignored_if_it_is_an_OPTIONS_request_returns_success()
    {
        // Arrange
        var http = MockHttps.HttpRequest("OPTIONS",
            (TusHeaderNames.Resumable, "0.2.2")
        );
        var request = RequestContext.Create(http, CancellationToken.None);

        // Act
        var response = request.Bind(c => CommonRequestHandler.CheckTusVersion(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async void Existing_metadata_file_returns_success()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo();
        var uploadStorageHandler = MockHandlers.UploadStorageHandler(file);
        var http = MockHttps.HttpRequest("SOME_HTTP_METHOD",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = new CommonRequestHandler(uploadStorageHandler);

        // Act
        var response = await request.BindAsync(async c => await handler.CheckUploadFileInfoExistsAsync(c));
        var result = response.IsSuccess();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async void Non_existing_metadata_file_returns_404_error()
    {
        // Arrange
        var uploadStorageHandler = MockHandlers.UploadStorageHandler(file: null);
        var http = MockHttps.HttpRequest("SOME_HTTP_METHOD",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var request = RequestContext.Create(http, CancellationToken.None);
        var handler = new CommonRequestHandler(uploadStorageHandler);

        // Act
        var response = await request.BindAsync(async c => await handler.CheckUploadFileInfoExistsAsync(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 404, result);
    }
}
