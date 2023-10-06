using System;
using Microsoft.AspNetCore.Routing;
using SolidTUS.Extensions;
using SolidTUS.Models;

namespace SolidTUS.ProtocolHandlers.ProtocolExtensions;

/// <summary>
/// Termination request handler
/// </summary>
internal class TerminationRequestHandler
{
    private readonly LinkGenerator linkGenerator;

    /// <summary>
    /// Instantiate a new object of <see cref="TerminationRequestHandler"/>
    /// </summary>
    /// <param name="linkGenerator">The link generator</param>
    public TerminationRequestHandler(
        LinkGenerator linkGenerator
    )
    {
        this.linkGenerator = linkGenerator;
    }

    /// <summary>
    /// Validate that the route to both upload and delete are the same
    /// </summary>
    /// <param name="context">The request context</param>
    /// <param name="deleteRouteName">The route name to delete</param>
    /// <param name="uploadRouteName">The route name to upload</param>
    /// <param name="routeValues">The route values for either</param>
    /// <returns>A request context result</returns>
    /// <exception cref="InvalidOperationException">Thrown if routes mismatch</exception>
    public Result<TusResult> ValidateRoute(TusResult context, string? deleteRouteName, string? uploadRouteName, RouteValueDictionary routeValues)
    {
        if (deleteRouteName is null || uploadRouteName is null)
        {
            throw new InvalidOperationException("Must have route name for each upload endpoint and the delete endpoint");
        }

        var uploadPath = linkGenerator.GetPathByName(uploadRouteName, routeValues);
        var deletePath = linkGenerator.GetPathByName(deleteRouteName, routeValues);
        if (uploadPath == deletePath)
        {
            return context.Wrap();
        }

        throw new InvalidOperationException("Both routes for the upload endpoint and the delete endpoint must be the same route");
    }
}
