using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SolidTUS.Models;

namespace SolidTUS.Logging;

/// <summary>
/// Logging for upload-related operations in SolidTUS.
/// </summary>
public static partial class UploadLogs
{
    /// <summary>
    /// Logs the start of the expired uploads scanner job.
    /// This method is used to log when the background service starts scanning for expired uploads.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="interval">The interval for scan.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Starting expired uploads scanner job, every {Interval}"
    )]
    public static partial void LogStartScanExpiredUploads(
        this ILogger logger,
        TimeSpan interval
    );

    /// <summary>
    /// Logs the scanning for expired uploads.
    /// This method is used to log when the background service is actively scanning for expired uploads.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Scanning for expired uploads..."
    )]
    public static partial void LogScanningForExpiredUploads(
        this ILogger logger
    );

    /// <summary>
    /// Logs an error when a file ID is not provided for a resource.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Must provide file id for the resource"
    )]
    public static partial void LogMustProvideFileIdForResource(
        this ILogger logger
    );

    /// <summary>
    /// Logs an error when there is no upload endpoint available for a resource.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Must have an upload endpoint to upload the resource"
    )]
    public static partial void LogMustHaveUploadEndpointForResource(
        this ILogger logger
    );

    /// <summary>
    /// Logs the creation of a resource.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="uploadFileInfo"></param>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Created resource {@UploadFileInfo}"
    )]
    public static partial void LogCreatedResource(
        this ILogger logger,
        UploadFileInfo uploadFileInfo
    );

    /// <summary>
    /// Logs the completion of uploading a resource.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="uploadFileInfo">The upload file info.</param>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "Finished uploading resource {@UploadFileInfo}"
    )]
    public static partial void LogFinishedUploadingResource(
        this ILogger logger,
        UploadFileInfo uploadFileInfo
    );

    /// <summary>
    /// Logs an error when a resource could not be created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="uploadInfo">The upload file info.</param>
    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Error,
        Message = "Could not create upload resource {@UploadInfo}"
    )]
    public static partial void LogCouldNotCreateUploadResource(
        this ILogger logger,
        UploadFileInfo uploadInfo
    );

    /// <summary>
    /// Logs an error when a partial resource could not be found for merging.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="partialUrl"></param>
    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Error,
        Message = "Could not find partial resource with url {PartialUrl} for merging"
    )]
    public static partial void LogCouldNotFindPartialResourceForMerging(
        this ILogger logger,
        string partialUrl
    );

    /// <summary>
    /// Logs an error when a partial resource could not be found by its ID for merging.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="partialId"></param>
    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Error,
        Message = "Could not find partial resource with id {PartialId} for merging"
    )]
    public static partial void LogCouldNotFindPartialResourceByIdForMerging(
        this ILogger logger,
        string partialId
    );

    /// <summary>
    /// Logs an error when a partial resource has not finished uploading and cannot be merged.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="partialId">The partial file id.</param>
    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Error,
        Message = "Partial resource {PartialId} has not yet finished uploading, cannot merge unfinished uploads"
    )]
    public static partial void LogPartialResourceNotFinishedUploading(
        this ILogger logger,
        string partialId
    );

    /// <summary>
    /// Logs an error when attempting to merge non-partial files.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="file">The upload file info.</param>
    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Error,
        Message = "Cannot merge non-partial files {@File}"
    )]
    public static partial void LogCannotMergeNonPartialFiles(
        this ILogger logger,
        UploadFileInfo file
    );

    /// <summary>
    /// Logs an error when a merge operation is denied due to file conditions.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="files"></param>
    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Error,
        Message = "Denied merge of {@Files}"
    )]
    public static partial void LogDeniedMergeOfFiles(
        this ILogger logger,
        List<UploadFileInfo> files
    );

    /// <summary>
    /// Logs an error when merging partial files fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="partialFiles">The partial files.</param>
    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Error,
        Message = "Error occurred could not merge {@PartialFiles}"
    )]
    public static partial void LogErrorCouldNotMergePartialFiles(
        this ILogger logger,
        List<UploadFileInfo> partialFiles
    );

    /// <summary>
    /// Logs the successful merging of files into a final resource.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="finalInfo">The final file info.</param>
    /// <param name="partialInfos">The partial file infos.</param>
    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Information,
        Message = "Merged files into {@FinalInfo} with the {@PartialInfos}"
    )]
    public static partial void LogMergedFiles(
        this ILogger logger,
        UploadFileInfo finalInfo,
        List<UploadFileInfo> partialInfos
    );

    /// <summary>
    /// Logs an error when the upload file info could not be written.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="uploadFileInfo">The upload file info.</param>
    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Error,
        Message = "Could not write upload file info {@UploadFileInfo}"
    )]
    public static partial void LogCouldNotWriteUploadFileInfo(
        this ILogger logger,
        Exception exception,
        UploadFileInfo uploadFileInfo
    );

    /// <summary>
    /// Logs an error when the partial upload file info could not be written.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="partialUpload">The partial upload file info.</param>
    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Error,
        Message = "Could not write partial upload file info {@PartialUpload}"
    )]
    public static partial void LogCouldNotWritePartialUploadFileInfo(
        this ILogger logger,
        Exception exception,
        UploadFileInfo partialUpload
    );
}