using System;

using SolidTus.Tests.Mocks;

using SolidTUS.Models;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Mocks;

namespace SolidTus.Tests.ProtocolHandlerTests.COMMON;

public static class Setup
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
