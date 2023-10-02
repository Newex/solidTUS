using Microsoft.AspNetCore.Routing;

namespace SolidTUS.Wrappers;

/// <summary>
/// Wrapper for the <see cref="LinkGenerator"/>
/// </summary>
public interface ILinkGeneratorWrapper
{
    /// <summary>
    /// Generate a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="routeName">The route name</param>
    /// <param name="routeValues">The route values</param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    string? GetPathByName(string routeName, object? routeValues);
}

/// <summary>
/// Link generator implementation
/// </summary>
public class LinkGeneratorWrapper : ILinkGeneratorWrapper
{
    private readonly LinkGenerator linkGenerator;

    /// <summary>
    /// Instantiate a new wrapper
    /// </summary>
    /// <param name="linkGenerator">The link generator</param>
    public LinkGeneratorWrapper(LinkGenerator linkGenerator)
    {
        this.linkGenerator = linkGenerator;

    }

    /// <inheritdoc />
    public string? GetPathByName(string routeName, object? routeValues) =>
        linkGenerator.GetPathByName(routeName, routeValues);

}
