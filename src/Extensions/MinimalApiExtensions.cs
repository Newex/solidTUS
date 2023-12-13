using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SolidTUS.Builders;
using SolidTUS.Constants;
using SolidTUS.Contexts;
using SolidTUS.Filters;

namespace SolidTUS.Extensions;

public static class MinimalApiExtensions
{
    /// <summary>
    /// Maps an endpoint for TUS file upload.
    /// </summary>
    /// <remarks>
    /// Route must include a file id parameter.
    /// It is assumed that the filterId is the 2nd argument.
    /// </remarks>
    /// <param name="app">The web application</param>
    /// <param name="route">The route path</param>
    /// <param name="handler">The route handler</param>
    /// <param name="fileIdIndex">The fileId argument index</param>
    /// <returns>A route handler builder</returns>
    public static RouteHandlerBuilder MapTusUpload(this WebApplication app,
                                                   [StringSyntax("Route")] string route,
                                                   Delegate handler,
                                                   int fileIdIndex = 1)
    {
        return app
            .Map(route, handler)
            .AddEndpointFilter(new TusUploadFilter(fileIdIndex));
    }
}