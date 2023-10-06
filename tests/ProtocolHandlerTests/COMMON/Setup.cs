using SolidTUS.Tests.Mocks;
using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;

namespace SolidTUS.Tests.ProtocolHandlerTests.COMMON;

internal static class Setup
{
    public static CommonRequestHandler CommonRequestHandler(UploadFileInfo? fileInfo = null)
    {
        var uploadStorageHandler = MockHandlers.UploadStorageHandler();
        var uploadMetaHandler = MockHandlers.UploadMetaHandler(fileInfo);
        var clock = MockOthers.Clock();
        return new CommonRequestHandler(
            uploadStorageHandler,
            uploadMetaHandler,
            clock
        );
    }
}
