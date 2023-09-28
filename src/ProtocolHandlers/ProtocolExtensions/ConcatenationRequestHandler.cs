using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;


using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Handlers;
using SolidTUS.Models;
using SolidTUS.Options;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

public class ConcatenationRequestHandler
{
    private readonly IUploadMetaHandler uploadMetaHandler;
    private readonly long? maxSize;

    public ConcatenationRequestHandler(
        IUploadMetaHandler uploadMetaHandler,
        IOptions<TusOptions> options
    )
    {
        this.uploadMetaHandler = uploadMetaHandler;
        maxSize = options.Value.MaxSize;
    }

    public RequestContext SetIfUploadIsPartial(RequestContext context)
    {
        context.UploadFileInfo.IsPartial = context.RequestHeaders.Any(
            x => x.Key == TusHeaderNames.UploadConcat
                 && x.Value == TusHeaderValues.UploadPartial);

        return context;
    }

    public async Task<Result<RequestContext>> CheckIfUploadPartialIsFinalAsync(RequestContext context, string template, string partialParameterName, CancellationToken cancellationToken)
    {
        var concat = context.RequestHeaders[TusHeaderNames.UploadConcat].ToString();
        var isFinal = concat.StartsWith(TusHeaderValues.UploadFinal, StringComparison.OrdinalIgnoreCase);
        var uploadInfos = new List<UploadFileInfo>();
        HttpError? error = null;
        if (isFinal)
        {
            // final;
            var list = concat[(concat.IndexOf(';') + 1)..];
            if (list.Length == 0)
            {
                return HttpError.BadRequest("Must provide a list of files to concatenate").Wrap();
            }

            var partialIds = new string[list.Length];
            var pathCount = 0;
            var start = 0;
            for (var i = 0; i < list.Length; i++)
            {
                if (i == list.Length - 1)
                {
                    var path = list[start..];
                    if (path is null)
                    {
                        break;
                    }

                    var value = GetTemplateValue(path.ToString(), template, partialParameterName);
                    if (value is not null)
                    {
                        partialIds[pathCount] = value;
                    }
                }

                if (list[i] == ' ')
                {
                    var path = list[start..i];
                    if (path is null)
                    {
                        continue;
                    }

                    var value = GetTemplateValue(path.ToString(), template, partialParameterName);
                    if (value is not null)
                    {
                        partialIds[pathCount] = value;
                        pathCount++;
                        start = i + 1;
                    }
                }
            }

            if (partialIds.Length == 0)
            {
                error = HttpError.BadRequest("Missing partial uploads for final concatenation");
            }
            else
            {
                var partialTotalSize = 0L;
                foreach (var partialId in partialIds)
                {
                    if (partialId is null)
                    {
                        break;
                    }

                    var info = await uploadMetaHandler.GetResourceAsync(partialId, cancellationToken);
                    if (info is not null)
                    {
                        if (uploadInfos.Exists(x => x.FileId == partialId))
                        {
                            error = HttpError.BadRequest("Can only use partial upload once per concatenation");
                            break;
                        }

                        if (!info.Done)
                        {
                            error = HttpError.Forbidden("Can only merge partial uploads that are fully uploaded");
                            break;
                        }

                        uploadInfos.Add(info);
                        partialTotalSize += info.ByteOffset;

                        if (maxSize.HasValue && partialTotalSize > maxSize.Value)
                        {
                            error = HttpError.EntityTooLarge("Partial files exceed Tus-Max-Size limit");
                            break;
                        }
                    }
                    else
                    {
                        error = HttpError.NotFound($"Could not find file: {partialId} used in concatenation");
                        break;
                    }
                }

                context.PartialFinalUploadInfos = uploadInfos;
            }
        }

        if (error.HasValue)
        {
            return error.Value.Wrap();
        }

        return context.Wrap();
    }

    public static string? GetTemplateValue(string input, string template, string token)
    {
        string pattern = Regex.Escape(template).Replace("\\{", "{").Replace("\\}", "}");
        // pattern = Regex.Replace(pattern, @"{([^}]+)}", @"(?<$1>[^/]+)", RegexOptions.None);
        pattern = Regex.Replace(pattern, @"{([^:]+)(:[^}]+)?}", @"(?<$1>[^/]+)");

        var match = Regex.Match(input, pattern);
        if (match.Success && match.Groups[token].Success)
        {
            return match.Groups[token].Value;
        }

        return null;
    }
}

