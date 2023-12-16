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
    /// <param name="fileIdRouteValue">The file id route value</param>
    /// <param name="routeValues">The route values</param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    string? GetPathByName(string routeName, (string, object) fileIdRouteValue, params (string Key, object Value)[] routeValues);

    /// <summary>
    /// Get path to the upload endpoint with the new given key and value
    /// </summary>
    /// <param name="key">The key identifier</param>
    /// <param name="value">The route value</param>
    /// <param name="routeName">The route name</param>
    /// <returns>A string to the upload endpoint</returns>
    string? GetPathToUploadWithWhenKey(string key, object value, string routeName);
}

/// <summary>
/// Link generator implementation
/// </summary>
public class LinkGeneratorWrapper : ILinkGeneratorWrapper
{
    private readonly LinkGenerator linkGenerator;
    private readonly RouteValueDictionary routeValueDictionary = [];

    /// <summary>
    /// Instantiate a new wrapper
    /// </summary>
    /// <param name="linkGenerator">The link generator</param>
    public LinkGeneratorWrapper(LinkGenerator linkGenerator)
    {
        this.linkGenerator = linkGenerator;
    }

    /// <inheritdoc />
    public string? GetPathByName(string routeName, (string, object) fileIdRouteValue, params (string, object)[] routeValues)
    {
        foreach (var (key, value) in routeValues)
        {
            routeValueDictionary.TryAdd(key, value);
        }

        routeValueDictionary.TryAdd(fileIdRouteValue.Item1, fileIdRouteValue.Item2);
        return linkGenerator.GetPathByName(routeName, routeValueDictionary);
    }

    /// <inheritdoc />
    public string? GetPathToUploadWithWhenKey(string key, object value, string routeName)
    {
        routeValueDictionary[key] = value;
        return linkGenerator.GetPathByName(routeName, routeValueDictionary);
    }
}
