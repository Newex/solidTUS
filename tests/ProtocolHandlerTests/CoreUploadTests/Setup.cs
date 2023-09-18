using System.Collections.Generic;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Validators;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

public static class Setup
{
    public static UploadFlow UploadFlow(IUploadMetaHandler? uploadMetaHandler = null, UploadFileInfo? file = null)
    {
        var storageHandler = MockHandlers.UploadStorageHandler(currentSize: file?.ByteOffset);
        var upload = uploadMetaHandler ?? MockHandlers.UploadMetaHandler(file);
        var common = new CommonRequestHandler(storageHandler, upload);
        var patch = new PatchRequestHandler(upload);
        var checksum = new ChecksumRequestHandler(new List<IChecksumValidator>());
        return new UploadFlow(
            common,
            patch,
            checksum,
            storageHandler,
            upload
        );
    }
}
