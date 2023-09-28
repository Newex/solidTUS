using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SolidTUS.Models;

namespace SolidTUS.Handlers;

/// <summary>
/// Expired upload handler
/// </summary>
public interface IExpiredUploadHandler
{
    /// <summary>
    /// Handle single expired upload
    /// </summary>
    /// <param name="uploadFileInfo">The expired upload</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An awaitable task</returns>
    Task ExpiredUploadAsync(UploadFileInfo uploadFileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Start scanning for expired uploads
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>An awaitable task</returns>
    Task StartScanForExpiredUploadsAsync(CancellationToken cancellationToken);
}
