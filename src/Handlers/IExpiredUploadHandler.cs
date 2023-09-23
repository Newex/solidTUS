using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// Expired upload handler
/// </summary>
public interface IExpiredUploadHandler
{
    /// <summary>
    /// Handle expired single expired upload
    /// </summary>
    /// <param name="uploadFileInfo">The expired upload</param>
    /// <returns>An awaitable task</returns>
    Task ExpiredUploadAsync(UploadFileInfo uploadFileInfo);

    /// <summary>
    /// Start scanning for expired uploads
    /// </summary>
    /// <returns></returns>
    Task StartScanForExpiredUploadsAsync();
}
