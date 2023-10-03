using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using SolidTUS.Contexts;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolFlows;
using SolidTUS.ProtocolHandlers;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using SolidTUS.Validators;

namespace SolidTUS.Tests.ProtocolHandlerTests.CoreUploadTests;

public static class Setup
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

    public static TusCreationContext TusCreationContext(
        bool withUpload = false,
        long bytesWritten = 0L,
        PipeReader? reader = null,
        PartialMode partialMode = PartialMode.None,
        List<string>? urls = null,
        string? url = null,
        UploadFileInfo? fileInfo = null,
        Action<string>? onCreated = null,
        Action<long>? onUpload = null,
        IUploadStorageHandler? uploadStorageHandler = null,
        IUploadMetaHandler? uploadMetaHandler = null,
        CancellationToken? cancellationToken = null)
    {

        reader ??= new Pipe().Reader;
        var fakeFileInfo = fileInfo ?? Fakes.RandomEntities.UploadFileInfo();
        var createCallback = onCreated ?? ((s) => { });
        var uploadCallback = onUpload ?? ((l) => { });
        urls ??= new List<string>();
        var storageHandler = uploadStorageHandler ?? MockHandlers.UploadStorageHandler(currentSize: fakeFileInfo.ByteOffset, bytesWritten: bytesWritten);
        var metaHandler = uploadMetaHandler ?? MockHandlers.UploadMetaHandler(fakeFileInfo);
        var linkGenerator = MockOthers.LinkGenerator(url);
        var cancel = cancellationToken ?? CancellationToken.None;
        var ioptions = Microsoft.Extensions.Options.Options.Create(new TusOptions());

        return new TusCreationContext(
            withUpload,

            partialMode,
            urls,

            fakeFileInfo,
            createCallback,
            uploadCallback,
            reader,
            storageHandler,
            metaHandler,
            linkGenerator,
            cancel,
            ioptions
        );
    }
}
