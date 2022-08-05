using System.IO;
using System.Threading;
using Moq;
using SolidTUS.Handlers;
using SolidTUS.Models;

namespace SolidTUS.Tests.Mocks;

public static class MockHandlers
{
    public static IUploadStorageHandler UploadStorageHandler(bool discarded = true)
    {
        var mock = new Mock<IUploadStorageHandler>();

        mock.Setup(s => s.GetPartialUploadedStreamAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new MemoryStream());

        mock.Setup(s => s.OnDiscardPartialUploadAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(discarded);

        return mock.Object;
    }

    public static IUploadMetaHandler UploadMetaHandler(UploadFileInfo? file = null, bool setFileSize = true)
    {
        var mock = new Mock<IUploadMetaHandler>();

        mock.Setup(s => s.GetUploadFileInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(file);

        mock.Setup(s => s.SetFileSizeAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(setFileSize);

        return mock.Object;
    }
}
