using SolidTUS.Parsers;

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

        // Act
        var result = MetadataParser.Parse(input);

        // Assert
        Assert.Collection(result,
            first => Assert.Equal(Value1, first.Value),
            second => Assert.Equal(Value2, second.Value),
            third => Assert.Equal(string.Empty, third.Value));
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

        // Act
        var result = MetadataParser.Parse(input);

        // Assert
        Assert.Collection(result,
            first => Assert.Equal(Value1, first.Value),
            second => Assert.Equal(string.Empty, second.Value),
            third => Assert.Equal(Value3, third.Value));
    }

    [Fact]
    public void Fast_parse_even_with_empty_value_in_the_middle_and_no_spaces()
    {
        // Arrange
        const string Key1 = "Filename";
        const string Value1 = "how_to.pdf";
        const string Key2 = "Empty";
        const string Key3 = "MimeType";
        const string Value3 = "application/pdf";
        var input = $"{Key1} {Base64Converters.Encode(Value1)},{Key2},{Key3} {Base64Converters.Encode(Value3)}";

        // Act
        var result = MetadataParser.ParseFast(input);

        // Assert
        Assert.Collection(result,
            first => Assert.Equal(Value1, first.Value),
            second => Assert.Equal(string.Empty, second.Value),
            third => Assert.Equal(Value3, third.Value));
    }

    [Fact]
    public void Fast_parse_can_parse_metadata()
    {
        // Arrange
        const string Key1 = "Filename";
        const string Value1 = "how_to.pdf";
        const string Key2 = "MimeType";
        const string Value2 = "application/pdf";
        const string Key3 = "Empty";
        var input = $"{Key1} {Base64Converters.Encode(Value1)},{Key2} {Base64Converters.Encode(Value2)},{Key3}";

        // Act
        var result = MetadataParser.ParseFast(input);

        // Assert
        Assert.Collection(result,
            first => Assert.Equal(Value1, first.Value),
            second => Assert.Equal(Value2, second.Value),
            third => Assert.Equal(string.Empty, third.Value));
    }
}
