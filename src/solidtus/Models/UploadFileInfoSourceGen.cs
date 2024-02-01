using System.Text.Json.Serialization;

namespace SolidTUS.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UploadFileInfo))]
internal partial class UploadFileInfoSourceGen : JsonSerializerContext
{
}
