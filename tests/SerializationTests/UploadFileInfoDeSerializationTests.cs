using System.Text.Json;
using SolidTUS.Models;

namespace SolidTUS.Tests.SerializationTests;

public class UploadFileInfoDeSerializationTests
{
    const string Json = "{\"ByteOffset\":10240000,\"FileSize\":785383424,\"Metadata\":{\"name\":\"fedora-coreos-37.20230218.3.0-live.x86_64.iso\",\"type\":\"application/octet-stream\",\"filetype\":\"application/octet-stream\",\"filename\":\"fedora-coreos-37.20230218.3.0-live.x86_64.iso\"},\"RawMetadata\":\"name ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv,type YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filetype YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filename ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv\",\"FileDirectoryPath\":\"./FileUploads\",\"OnDiskFilename\":\"96bf59ff41ac4ecda1e2ff9046a06794\",\"ExpirationStrategy\":\"SlidingExpiration\",\"Interval\":\"00:00:30\",\"CreatedDate\":\"2023-09-22T17:26:07.2383739+00:00\",\"Done\":false}";

    [Fact]
    public void UploadFileInfo_text_deserialization_should_deserialize_expiration_enum()
    {
        var uploadFileInfo = JsonSerializer.Deserialize<UploadFileInfo>(Json);

        uploadFileInfo.Should()
                      .Match<UploadFileInfo>(f => f.ExpirationStrategy == ExpirationStrategy.SlidingExpiration);
    }
}
