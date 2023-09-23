using System.IO.Pipelines;
using System.Threading;
using Moq;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;

namespace SolidTUS.Tests.Mocks;

public static class MockHandlers
{
    public static IUploadStorageHandler UploadStorageHandler(long? currentSize = null, long bytesWritten = 0)
    {
        var mock = new Mock<IUploadStorageHandler>();

        mock.Setup(s => s.GetUploadSize(It.IsAny<string>(), It.IsAny<UploadFileInfo>()))
        .Returns(currentSize);

        mock.Setup(s => s.OnPartialUploadAsync(It.IsAny<PipeReader>(), It.IsAny<UploadFileInfo>(), It.IsAny<ChecksumContext>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(bytesWritten);

        return mock.Object;
    }

    public static IUploadMetaHandler UploadMetaHandler(UploadFileInfo? file = null, bool updated = true, bool createdInfo = true)
    {
        var mock = new Mock<IUploadMetaHandler>();

        mock.Setup(s => s.GetResourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(file);

        mock.Setup(s => s.UpdateResourceAsync(It.IsAny<UploadFileInfo>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(updated);

        mock.Setup(s => s.CreateResourceAsync(It.IsAny<UploadFileInfo>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(createdInfo);

        return mock.Object;
    }

    public static IExpiredUploadHandler ExpiredUploadHandler()
    {
        var mock = new Mock<IExpiredUploadHandler>();

        mock.Setup(x => x.ExpiredUploadAsync(It.IsAny<UploadFileInfo>()));

        return mock.Object;
    }
}
