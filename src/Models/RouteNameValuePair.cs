namespace SolidTUS.Models;

/// <summary>
/// Route data containing info about the route name and the route values
/// </summary>
/// <param name="RouteName">The route name</param>
/// <param name="RouteValues">The route values</param>
public record struct RouteNameValuePair(string? RouteName, object? RouteValues);