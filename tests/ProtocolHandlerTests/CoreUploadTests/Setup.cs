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
    public static UploadFlow UploadFlow(IUploadStorageHandler? uploadStorageHandler = null)
    {
        var upload = uploadStorageHandler ?? MockHandlers.UploadStorageHandler();
        var common = new CommonRequestHandler(upload);
        var patch = new PatchRequestHandler(upload);
        var checksum = new ChecksumRequestHandler(new List<IChecksumValidator>());
        return new UploadFlow(
            common,
            patch,
            checksum,
            upload
        );
    }
}
