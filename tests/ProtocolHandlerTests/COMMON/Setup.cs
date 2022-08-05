using System;
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
        return new CommonRequestHandler(
            uploadStorageHandler,
            uploadMetaHandler
        );
    }
}
