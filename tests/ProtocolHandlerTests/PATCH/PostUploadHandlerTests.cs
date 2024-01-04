using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Options;
using SolidTUS.Tests.Fakes;
using SolidTUS.Tests.Mocks;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.PATCH;

[UnitTest]
public class PostUploadHandlerTests
{
    /// <summary>
    /// Github issue #6.
    /// An unfinished upload should return success.
    /// </summary>
    [Fact]
    public async void Unfinished_successful_upload_must_return_success()
    {
        // Arrange
        var clock = MockOthers.Clock();
        var tusOptions = MSOptions.Create(new TusOptions());
        var metaHandler = MockHandlers.UploadMetaHandler();
        var storageHandler = MockHandlers.UploadStorageHandler();
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var uploadHandler = new UploadHandler(
            clock,
            tusOptions,
            metaHandler,
            storageHandler,
            expiredHandler
        );

        var pipeReader = new Pipe().Reader;
        var tusUploadContext = new TusUploadContext("fileId", (f) => Task.CompletedTask);
        var file = RandomEntities.UploadFileInfo() with
        {
            FileId = "fileId",

            // 50 bytes uploaded so far, out of 100 bytes = unfinished upload
            ByteOffset = 50L,
            FileSize = 100L,
        };
        var tusResult = MockOthers.TusResult(file);
        var cancel = CancellationToken.None;

        // Act
        var act = async () => await uploadHandler.HandleUploadAsync(pipeReader, tusUploadContext, tusResult, cancel);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
