using CSharpFunctionalExtensions;
using SolidTUS.Builders;
using SolidTUS.Models;
using SolidTUS.Parsers;
using SolidTUS.Validators;

namespace SolidTUS.Tests.ParserTests.MetadataParserTests;

public class ParseInputToDictionary
{
    [Fact]
    public void Can_parse_metadata()
    {
        // Arrange
        const string Key1 = "Filename";
        const string Value1 = "how_to.pdf";
        const string Key2 = "MimeType";
        const string Value2 = "application/pdf";
        const string Key3 = "Empty";
        var input = $"{Key1} {Base64Converters.Encode(Value1)},{Key2} {Base64Converters.Encode(Value2)},{Key3}";
        var parser = new MetadataParser(MetadataValidator.Validator, MetadataValidator.AllowEmptyMetadata);

        // Act
        var result = parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Can_parse_even_with_empty_value_in_the_middle_and_no_spaces()
    {
        // Arrange
        const string Key1 = "Filename";
        const string Value1 = "how_to.pdf";
        const string Key2 = "Empty";
        const string Key3 = "MimeType";
        const string Value3 = "application/pdf";
        var input = $"{Key1} {Base64Converters.Encode(Value1)},{Key2},{Key3} {Base64Converters.Encode(Value3)}";
        var parser = new MetadataParser(MetadataValidator.Validator, MetadataValidator.AllowEmptyMetadata);

        // Act
        var result = parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
