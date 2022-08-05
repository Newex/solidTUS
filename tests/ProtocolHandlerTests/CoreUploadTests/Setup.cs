using System.Collections.Generic;
using SolidTUS.Handlers;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Validators;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

public static class Setup
{
    public static UploadFlow UploadFlow(IUploadMetaHandler? uploadMetaHandler = null)
    {
        var storageHandler = MockHandlers.UploadStorageHandler();
        var upload = uploadMetaHandler ?? MockHandlers.UploadMetaHandler();
        var common = new CommonRequestHandler(storageHandler, upload);
        var patch = new PatchRequestHandler(upload);
        var checksum = new ChecksumRequestHandler(new List<IChecksumValidator>());
        return new UploadFlow(
            common,
            patch,
            checksum,
            storageHandler
        );
    }
}
