using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;
using SolidTUS.Validators;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

public class PreUploadRequestTests
{
    [Fact]
    public async Task Request_should_be_successful()
    {
        /*
        * We currently get an error when sending a valid request
        * ERROR: "Missing either Upload-Length or Content-Length header"
        * Sent headers:
            Content-Length: 261794474
            Content-Type: application/offset+octet-stream
            Tus-Resumable: 1.0.0
            Upload-Concat: partial
            Upload-Offset: 0
        */

        // Setup all Handlers for UploadFlow
        var fileSize = 261794474L;
        var fileId = "file_123";

        // File info
        var info = new UploadFileInfo
        {
            FileId = fileId,
            IsPartial = true,
            FileSize = fileSize
        };
        var uploadStorageHandler = MockHandlers.UploadStorageHandler();
        var uploadMetaHandler = MockHandlers.UploadMetaHandler(file: info);
        var clock = MockOthers.Clock();
        var tusOptions = MSOptions.Create(new TusOptions());
        var expiredHandler = MockHandlers.ExpiredUploadHandler();

        var common = new CommonRequestHandler(uploadStorageHandler, uploadMetaHandler, clock);
        var patch = new PatchRequestHandler();
        var checksum = new ChecksumRequestHandler(Enumerable.Empty<IChecksumValidator>());
        var expiration = new ExpirationRequestHandler(clock, expiredHandler, tusOptions);

        var sut = new UploadFlow(
            common,
            patch,
            checksum,
            expiration,
            uploadStorageHandler,
            uploadMetaHandler
        );

        // Setup request with the headers
        var http = MockHttps.HttpRequest("PATCH",
            ("Content-Length", fileSize.ToString()),
            ("Content-Type", "application/offset+octet-stream"),
            ("Tus-Resumable", "1.0.0"),
            ("Upload-Concat", "partial"),
            ("Upload-Offset", "0")
        );

        // TusResult
        var context = TusResult.Create(http, new Mock<HttpResponse>().Object).Value;


        // Act
        var pre = await sut.PreUploadAsync(context!, fileId, CancellationToken.None);
        var result = pre.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }
}
