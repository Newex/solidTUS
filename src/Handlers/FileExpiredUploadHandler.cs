using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SolidTUS.Models;

namespace SolidTUS.Handlers;

public class FileExpiredUploadHandler : IExpiredUploadHandler
{
    private readonly IUploadMetaHandler uploadMetaHandler;

    private FileExpiredUploadHandler(
        IUploadMetaHandler uploadMetaHandler
     )
    {
        this.uploadMetaHandler = uploadMetaHandler;

    }

    /// <inheritdoc />
    public async Task ExpiredUploadAsync(UploadFileInfo uploadFileInfo)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task StartScanForExpiredUploadsAsync()
    {
        throw new NotImplementedException();
    }

}
