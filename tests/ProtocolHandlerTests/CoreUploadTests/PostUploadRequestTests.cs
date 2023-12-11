using System.Linq;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.Parsers;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Tests.Tools;
using SolidTUS.Validators;

using MSOptions = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

public class PostUploadRequestTests
{
    [Fact]
    public void Upload_final_merge_should_be_valid_for_partial_files()
    {
        /**
        * Request:
            POST /api/upload
            Tus-Resumable: 1.0.0
            Upload-Concat: final;https://localhost:7134/af99e9c0 https://localhost:7134/1e84ab2c https://localhost:7134/e56db291
            Upload-Metadata: name ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv,type YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filetype YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filename ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv
            Cache-Control: no-cache

        * Actual response:
            HTTP/2 400
            [BODY]
            Must have either Upload-Length or Upload-Defer-Length header and not both

        * Expected response:
            201 Created
            Location: https://localhost:7134/new_id
            Tus-Resumable: 1.0.0
        */

        var clock = MockOthers.Clock();
        var tusOptions = MSOptions.Create(new TusOptions());
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var parser = new MetadataParser(_ => true, () => true);

        var post = new PostRequestHandler(parser, tusOptions);
        var checksum = new ChecksumRequestHandler(Enumerable.Empty<IChecksumValidator>());
        var expiration = new ExpirationRequestHandler(clock, expiredHandler, tusOptions);

        var sut = new CreationFlow(
            post,
            checksum,
            expiration
        );

        // Setup request with the headers
        var request = MockHttps.HttpRequest("POST",
            ("Content-Length", "0"),
            ("Tus-Resumable", "1.0.0"),
            ("Upload-Concat", "final;https://localhost:7134/A https://localhost:7134/B https://localhost:7134/C"),
            ("Upload-Metadata", "name ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv,type YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filetype YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filename ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv")
        );
        var response = MockHttps.HttpResponse();

        // TusResult
        var context = TusResult.Create(request, response).Value;

        // Act
        var pre = sut.PreResourceCreation(context!);
        var result = pre.IsSuccess();

        // Assert
        result.Should().BeTrue();
    }
}
