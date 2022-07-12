using System.Collections.Generic;
using System.Linq;
using SolidTUS.Constants;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers;
using SolidTUS.Validators;
using P = Microsoft.Extensions.Options.Options;

namespace SolidTUS.Tests.ProtocolHandlerTests.OPTIONS;

[UnitTest]
public class ResponseTests
{
    [Fact]
    public void Response_includes_Tus_Resumable_header()
    {
        // Arrange
        var options = P.Create(new TusOptions());
        var validators = new List<IChecksumValidator>();
        var handler = new OptionsRequestHandler(options, validators);

        // Act
        var response = handler.ServerFeatureAnnouncements();
        var result = response.Headers.ContainsKey(TusHeaderNames.Resumable);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Response_does_not_contain_Tus_Max_Size_if_not_set()
    {
        // Arrange
        var options = P.Create(new TusOptions
        {
            MaxSize = null
        });
        var validators = new List<IChecksumValidator>();
        var handler = new OptionsRequestHandler(options, validators);

        // Act
        var response = handler.ServerFeatureAnnouncements();
        var result = response.Headers.ContainsKey(TusHeaderNames.MaxSize);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Response_does_contain_Tus_Max_Size_if_set()
    {
        // Arrange
        var options = P.Create(new TusOptions
        {
            MaxSize = 1000L
        });
        var validators = new List<IChecksumValidator>();
        var handler = new OptionsRequestHandler(options, validators);

        // Act
        var response = handler.ServerFeatureAnnouncements();
        var result = response.Headers.ContainsKey(TusHeaderNames.MaxSize);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Response_contains_support_for_protocol_extension_creation()
    {
        // Arrange
        var options = P.Create(new TusOptions());
        var validators = new List<IChecksumValidator>();
        var handler = new OptionsRequestHandler(options, validators);

        // Act
        var response = handler.ServerFeatureAnnouncements();
        var hasExtensions = response.Headers.TryGetValue(TusHeaderNames.Extension, out var extensions);
        var result = extensions
            .ToString()
            .Split(",")
            .Any(extension => extension.Equals("creation"));

        // Assert
        Assert.True(result);
    }
}
