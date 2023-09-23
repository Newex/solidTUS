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

    public static TusCreationContext TusCreationContext(bool withUpload,
                                                        PipeReader reader,
                                                        long bytesWritten = 0L,
                                                        FileStorageOptions? options = null,
                                                        UploadFileInfo? fileInfo = null,
                                                        Action<string>? onCreated = null,
                                                        Action<long>? onUpload = null,
                                                        IUploadStorageHandler? uploadStorageHandler = null,
                                                        IUploadMetaHandler? uploadMetaHandler = null,
                                                        CancellationToken? cancellationToken = null)
    {
        var fileOptions = Microsoft.Extensions.Options.Options.Create(options ?? new FileStorageOptions());
        var fakeFileInfo = fileInfo ?? Fakes.RandomEntities.UploadFileInfo();
        var createCallback = onCreated ?? ((s) => { });
        var uploadCallback = onUpload ?? ((l) => { });
        var storageHandler = uploadStorageHandler ?? MockHandlers.UploadStorageHandler(currentSize: fakeFileInfo.ByteOffset, bytesWritten: bytesWritten);
        var metaHandler = uploadMetaHandler ?? MockHandlers.UploadMetaHandler(fakeFileInfo);
        var cancel = cancellationToken ?? CancellationToken.None;

        return new TusCreationContext(
            fileOptions,
            withUpload,
            fakeFileInfo,
            createCallback,
            uploadCallback,
            reader,
            storageHandler,
            metaHandler,
            cancel
        );
    }
}
