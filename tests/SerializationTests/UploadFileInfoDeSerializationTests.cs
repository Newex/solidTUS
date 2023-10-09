using System;
using System.Text.Json;
using SolidTUS.Models;

namespace SolidTUS.Tests.SerializationTests;

[UnitTest]
public class UploadFileInfoDeSerializationTests
{
    const string Json = "{\"ByteOffset\":10240000,\"FileSize\":785383424,\"Metadata\":{\"name\":\"fedora-coreos-37.20230218.3.0-live.x86_64.iso\",\"type\":\"application/octet-stream\",\"filetype\":\"application/octet-stream\",\"filename\":\"fedora-coreos-37.20230218.3.0-live.x86_64.iso\"},\"RawMetadata\":\"name ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv,type YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filetype YXBwbGljYXRpb24vb2N0ZXQtc3RyZWFt,filename ZmVkb3JhLWNvcmVvcy0zNy4yMDIzMDIxOC4zLjAtbGl2ZS54ODZfNjQuaXNv\",\"FileDirectoryPath\":\"./FileUploads\",\"OnDiskFilename\":\"96bf59ff41ac4ecda1e2ff9046a06794\",\"CreatedDate\":\"2023-09-22T17:26:07.2383739+00:00\",\"Done\":false}";
    const string Partial = """{"FileId":"","PartialId":"f7236b52","ByteOffset":0,"FileSize":261794474,"IsPartial":true,"Metadata":{},"OnDiskFilename":"f7236b52","CreatedDate":"2023-10-02T05:09:16.3411431+00:00"}""";

    [Fact]
    public void UploadFileInfo_deserialization_should_include_CreatedDate_property()
    {
        var uploadFileInfo = JsonSerializer.Deserialize<UploadFileInfo>(Json);
        var createdDate = uploadFileInfo?.CreatedDate;
        createdDate.Should().NotBeNull();
    }

    [Fact]
    public void UploadFileInfo_deserialization_should_include_metadata_property()
    {
        var uploadFileInfo = JsonSerializer.Deserialize<UploadFileInfo>(Json);
        var metadata = uploadFileInfo?.Metadata;
        metadata.Should()
            .NotBeNull().And
            .HaveCount(4).And
            .ContainKey("name");
    }

    [Fact]
    public void UploadFileInfo_deserialization_should_deserialize_partial_info()
    {
        var uploadFileInfo = JsonSerializer.Deserialize<UploadFileInfo>(Partial);
        uploadFileInfo.Should().NotBeNull();
    }
}
