using System.Collections.Generic;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Validators;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

internal static class Setup
{
    public static UploadFlow UploadFlow(IUploadMetaHandler? uploadMetaHandler = null, UploadFileInfo? file = null, TusOptions? options = null)
    {
        var storageHandler = MockHandlers.UploadStorageHandler(currentSize: file?.ByteOffset);
        var upload = uploadMetaHandler ?? MockHandlers.UploadMetaHandler(file);
        var clock = MockOthers.Clock();
        var common = new CommonRequestHandler(storageHandler, upload, clock);
        var patch = new PatchRequestHandler();
        var checksum = new ChecksumRequestHandler(new List<IChecksumValidator>());
        var ioptions = Microsoft.Extensions.Options.Options.Create(options ?? new());
        var expirationHandler = MockHandlers.ExpiredUploadHandler();
        var expiration = new ExpirationRequestHandler(clock, expirationHandler, ioptions);
        return new UploadFlow(
            common,
            patch,
            checksum,
            expiration,
            storageHandler,
            upload
        );
    }
}
