using System.Linq;
using System.Threading;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.ConcatenationRequestTests;

[UnitTest]
public class ConcatenationRequestHandlerTests
{
    [Fact]
    public async void Should_not_allow_final_upload_to_reuse_partial_uploads_return_400_bad_request()
    {
        // Arrange
        var uploadInfo = Fakes.RandomEntities.UploadFileInfo() with { ByteOffset = 100, FileSize = 100 };
        var metaHandler = MockHandlers.UploadMetaHandler(uploadInfo);
        var http = MockHttps.HttpRequest("POST",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadConcat, TusHeaderValues.UploadFinal + ";partial/A partial/A")
        );
        var requestContext = RequestContext.Create(http, CancellationToken.None);
        var options = MSOptions.Create(new TusOptions());
        var concatenationHandler = new ConcatenationRequestHandler(metaHandler, options);

        // Act
        var response = await requestContext.BindAsync(
                async c => await concatenationHandler.CheckIfUploadPartialIsFinalAsync(c, "partial/{id}", "id"));
        var result = response.GetTusHttpResponse(204);

        // Assert
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async void Final_upload_should_not_exceed_tusmaxsize_header_constraint()
    {
        // Arrange
        var uploadInfo = Fakes.RandomEntities.UploadFileInfo() with
        {
            ByteOffset = 300L,
            FileSize = 300L
        };
        var metaHandler = MockHandlers.UploadMetaHandler(uploadInfo);
        var http = MockHttps.HttpRequest("POST",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadConcat, TusHeaderValues.UploadFinal + ";partial/A partial/B")
        );
        var requestContext = RequestContext.Create(http, CancellationToken.None);
        var options = MSOptions.Create(new TusOptions
        {
            MaxSize = 400L
        });
        var concatenationHandler = new ConcatenationRequestHandler(metaHandler, options);

        // Act
        var response = await requestContext.BindAsync(
                async c => await concatenationHandler.CheckIfUploadPartialIsFinalAsync(c, "partial/{id}", "id"));
        var result = response.GetTusHttpResponse(204);

        // Assert 413 request entity too large
        result.StatusCode.Should().Be(413);
    }

    [Fact]
    public async void Can_parse_upload_final_and_retrieve_info_metadata()
    {
        // Arrange
        var uploadInfo = Fakes.RandomEntities.UploadFileInfo() with { ByteOffset = 100L, FileSize = 100L };
        var metaHandler = MockHandlers.UploadMetaHandler(uploadInfo);
        var http = MockHttps.HttpRequest("POST",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion),
            (TusHeaderNames.UploadConcat, TusHeaderValues.UploadFinal + ";partial/A partial/B")
        );
        var requestContext = RequestContext.Create(http, CancellationToken.None);
        var options = MSOptions.Create(new TusOptions());
        var concatenationHandler = new ConcatenationRequestHandler(metaHandler, options);

        // Act
        var response = await requestContext.BindAsync(
                async c => await concatenationHandler.CheckIfUploadPartialIsFinalAsync(c, "partial/{id}", "id"));
        var uploads = response.Match(c => c.PartialFinalUploadInfos, _ => null);

        // Assert
        uploads.Should()
               .Match(x => x.Count() == 2)
               .And
               .ContainInOrder(uploadInfo with { FileId = "A" }, uploadInfo with { FileId = "B" });
    }

    [Fact]
    public async void No_upload_final_should_return_context_unchanged()
    {
        // Arrange
        var uploadInfo = Fakes.RandomEntities.UploadFileInfo() with { ByteOffset = 100L, FileSize = 100L };
        var metaHandler = MockHandlers.UploadMetaHandler(uploadInfo);
        var http = MockHttps.HttpRequest("POST",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var requestContext = RequestContext.Create(http, CancellationToken.None);
        var options = MSOptions.Create(new TusOptions());
        var concatenationHandler = new ConcatenationRequestHandler(metaHandler, options);

        // Act
        var response = await requestContext.BindAsync(
                async c => await concatenationHandler.CheckIfUploadPartialIsFinalAsync(c, "partial/{id}", "id"));
        var context = response.Match<RequestContext?>(c => c, _ => null);

        // Assert
        context.Should()
            .NotBeNull()
            .And
            .BeOfType<RequestContext>()
            .Which.PartialFinalUploadInfos
            .Should()
            .BeNull();
    }
}
