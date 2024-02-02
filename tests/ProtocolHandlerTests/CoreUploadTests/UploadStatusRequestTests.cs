using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SolidTUS.Models;
using SolidTUS.Tests.Mocks;
using MSOptions = Microsoft.Extensions.Options.Options;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Validators;
using SolidTUS.ProtocolFlows;
using System.Threading;
using SolidTUS.Models.Functional;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

[Feature("HEAD")]
public class UploadStatusRequestTests
{
    [Fact]
    public async Task Retrieving_status_for_valid_unfinished_upload_should_be_valid()
    {
        /*
        * We currently get a response where the Upload-Defer-Length header is set to 1
        * Sent headers:
            HEAD /be417f52 HTTP/2
            Tus-Resumable: 1.0.0
            Upload-Concat: partial

        * Expected response:
            Upload-Offset: 133997701
            Upload-Length: 261794474
            Cache-Control: no-store
            Tus-Resumable: 1.0.0

        * Actual response:
            upload-offset: 133997701
            upload-defer-length: 1
        */
        // Setup all Handlers for UploadFlow
        var fileSize = 261794474L;
        var offset = 133997701L;
        var fileId = "file_123";

        // File info
        var info = new UploadFileInfo
        {
            FileId = fileId,
            IsPartial = true,
            FileSize = fileSize
        };
        info.AddBytes(offset);
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
        var request = MockHttps.HttpRequest("HEAD",
            ("Content-Length", fileSize.ToString()),
            ("Tus-Resumable", "1.0.0"),
            ("Upload-Concat", "partial")
        );
        var responseHeaders = new Dictionary<string, string>();
        var response = MockHttps.HttpResponse(responseHeaders);

        // TusResult
        var (context, _) = TusResult.Create(request, response);


        // Act
        var (status, _) = await sut.GetUploadStatusAsync(context!, fileId, CancellationToken.None);
        var result = status?.ResponseHeaders.ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());

        // Assert
        result.Should().Contain(
            new KeyValuePair<string, string>("Cache-Control", "no-store"),
            new KeyValuePair<string, string>("Upload-Offset", offset.ToString()),
            new KeyValuePair<string, string>("Upload-Length", fileSize.ToString()),
            new KeyValuePair<string, string>("Tus-Resumable", "1.0.0")
        ).And.NotContain(
            new KeyValuePair<string, string>("Upload-Defer-Length", "1")
        );
    }
}
