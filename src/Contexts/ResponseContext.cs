using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using SolidTUS.Models;

namespace SolidTUS.Contexts;

public record ResponseContext
{
    public ResponseContext(IHeaderDictionary responseHeaders)
    {
        ResponseHeaders = responseHeaders;
    }

    public IHeaderDictionary ResponseHeaders { get; }

    public UploadFileInfo? UploadFileInfo { get; internal set; }

    public string? LocationUrl { get; internal set; }
}
