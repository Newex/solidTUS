using CSharpFunctionalExtensions;
using SolidTUS.Constants;
using SolidTUS.Options;
using SolidTUS.Parsers;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Tests.Tools;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.POST;

[UnitTest]
public class CreateCheckRequestTests
{
    [Fact]
    public void Request_without_any_upload_length_or_deferred_header_returns_400_status()
    {
        // Arrange
        var request = Setup.CreateRequest();

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Request_cannot_have_both_upload_length_and_deferred_header_returns_400_status()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 300L.ToString()),
            (TusHeaderNames.UploadDeferLength, "1")
        );

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void Defer_length_header_value_must_be_1_returns_400_on_invalid_status()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadDeferLength, "NotOne")
        );

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void When_upload_length_header_is_not_a_number_return_400_status()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, "NotANumber")
        );

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 400, result);
    }

    [Fact]
    public void When_upload_length_exceeds_max_size_return_413_status()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions
        {
            MaxSize = 200L
        });
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, "300")
        );
        var parser = new MetadataParser((_) => true, () => true);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var response = request.Bind(c => handler.CheckMaximumSize(c));
        var result = response.StatusCode();

        // Assert
        Assert.Equal(expected: 413, result);
    }

    [Fact]
    public void When_max_size_is_not_defined_then_allow_maximum_long_upload_and_return_201_status()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, long.MaxValue.ToString())
        );
        var parser = new MetadataParser((_) => true, () => true);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var response = request.Bind(c => handler.CheckMaximumSize(c));
        var result = response.StatusCode(onSuccess: 201);

        // Assert
        Assert.Equal(expected: 201, result);
    }

    [Fact]
    public void Get_parsed_metadata_on_success_if_it_is_included()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 123L.ToString()),
            (TusHeaderNames.UploadMetadata, "filename d29ybGRfZG9taW5hdGlvbl9wbGFuLnBkZg==,is_confidential")
        );
        var parser = new MetadataParser((_) => true, () => true);
        var handler = new PostRequestHandler(parser, MSOptions.Create(new TusOptions()));

        // Act
        var response = request.Map(handler.ParseAndValidateMetadata);

        // Assert
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Empty_metadata_is_not_valid_if_not_allowed()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true);
        var parser = new MetadataParser((_) => true, () => false);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var response = request.Bind(handler.ParseAndValidateMetadata);
        var result = response.StatusCode();

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Empty_metadata_is_valid_if_allowed()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 123L.ToString())
        );
        var parser = new MetadataParser((m) => m.ContainsKey("filename") && m.ContainsKey("is_confidential"), () => true);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var result = request.Bind(handler.ParseAndValidateMetadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Upload_Length_header_must_be_non_negative_returns_400_status_code()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, (-20).ToString())
        );

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        result.Should().Be(400);
    }

    [Fact]
    public void Partial_request_should_not_need_length()
    {
        // Arrange
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 20.ToString())
        );
        request = request.Map(c =>
        {
            c.PartialMode = Models.PartialMode.Partial;
            return c;
        });

        // Act
        var response = request.Bind(c => PostRequestHandler.CheckUploadLengthOrDeferred(c));
        var result = response.StatusCode();

        // Assert
        result.Should().Be(200);
    }

    [Fact]
    public void Normal_upload_should_validate_metadata()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 20.ToString()),
            (TusHeaderNames.UploadMetadata, "filename d29ybGRfZG9taW5hdGlvbl9wbGFuLnBkZg==,is_confidential")
        );
        request = request.Map(c =>
        {
            c.PartialMode = Models.PartialMode.None;
            return c;
        });
        
        var parser = new MetadataParser((m) => m.ContainsKey("filename"), () => false);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var result = request.Bind(handler.ParseAndValidateMetadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Partial_request_should_not_validate_metadata()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 20.ToString())
        );
        request = request.Map(c =>
        {
            c.PartialMode = Models.PartialMode.Partial;
            return c;
        });
        var parser = new MetadataParser((_) => false, () => false);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var result = request.Bind(handler.ParseAndValidateMetadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Final_parallel_upload_must_validate_metadata()
    {
        // Arrange
        var options = MSOptions.Create(new TusOptions());
        var request = Setup.CreateRequest(resumable: true,
            (TusHeaderNames.UploadLength, 20.ToString()),
            (TusHeaderNames.UploadMetadata, "filename d29ybGRfZG9taW5hdGlvbl9wbGFuLnBkZg==,is_confidential")
        );
        request = request.Map(c =>
        {
            c.PartialMode = Models.PartialMode.Final;
            return c;
        });
        var parser = new MetadataParser((m) => m.ContainsKey("filename"), () => true);
        var handler = new PostRequestHandler(parser, options);

        // Act
        var result = request.Bind(handler.ParseAndValidateMetadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
