using SolidTUS.Parsers;

namespace SolidTUS.Tests.ParserTests.ChecksumParserTests;

[UnitTest]
public class ChecksumValueParserTests
{
    [Fact]
    public void Parser_returns_null_if_there_is_not_exactly_1_space_between_algorithm_name_and_checksum_value()
    {
        // Arrange
        const string Input = "sha1_with_no_spaces";

        // Act
        var result = ChecksumValueParser.DecodeCipher(Input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parser_returns_null_if_invalid_base64_encoding()
    {
        // Arrange
        const string Input = "sha1 notbase64X-";

        // Act
        var result = ChecksumValueParser.DecodeCipher(Input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parser_returns_tuple_on_valid_format()
    {
        // Arrange
        const string Alg = "sha1";
        var cipher = Base64Converters.Encode("mySHA1 checksum");
        var input = $"{Alg} {cipher}";

        // Act
        var result = ChecksumValueParser.DecodeCipher(input);

        // Assert
        Assert.NotNull(result);
    }
}
