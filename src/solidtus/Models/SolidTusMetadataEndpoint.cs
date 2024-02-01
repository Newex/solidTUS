namespace SolidTUS.Models;

internal record SolidTusMetadataEndpoint(string Name, string Route, SolidTusEndpointType EndpointType);
internal enum SolidTusEndpointType
{
    Create,
    Upload,
}
