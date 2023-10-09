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
        var request = MockHttps.HttpRequest("GET",
            (TusHeaderNames.Resumable, "1.0.0"));
        var context = TusResult.Create(request, MockHttps.HttpResponse());
        

        // Act
        var response = context.Bind(CommonRequestHandler.CheckTusVersion);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Unsupported_TUS_version_returns_412_error()
    {
        // Arrange
        var request = MockHttps.HttpRequest("SOME-HTTP-METHOD",
            (TusHeaderNames.Resumable, "0.2.2"));
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(c => CommonRequestHandler.CheckTusVersion(c));
        var result = response.StatusCode();

        // Assert
        result.Should().Be(412);
    }

    [Fact]
    public void Unsupported_TUS_version_is_ignored_if_it_is_an_OPTIONS_request_returns_success()
    {
        // Arrange
        var request = MockHttps.HttpRequest("OPTIONS",
            (TusHeaderNames.Resumable, "0.2.2")
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());

        // Act
        var response = context.Bind(CommonRequestHandler.CheckTusVersion);
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async void Existing_metadata_file_returns_success()
    {
        // Arrange
        var file = RandomEntities.UploadFileInfo();
        var request = MockHttps.HttpRequest("SOME_HTTP_METHOD",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());
        var handler = Setup.CommonRequestHandler(file);

        // Act
        var response = await context.BindAsync(async c => await handler.SetUploadFileInfoAsync(c, file.FileId, CancellationToken.None));
        var result = response.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async void Non_existing_metadata_file_returns_404_error()
    {
        // Arrange
        var request = MockHttps.HttpRequest("SOME_HTTP_METHOD",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var context = TusResult.Create(request, MockHttps.HttpResponse());
        var handler = Setup.CommonRequestHandler();

        // Act
        var response = await context.BindAsync(async c => await handler.SetUploadFileInfoAsync(c, "nothing", CancellationToken.None));
        var result = response.StatusCode();

        // Assert
        result.Should().Be(404);
    }
}
