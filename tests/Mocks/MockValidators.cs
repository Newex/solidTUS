using System.IO;
using Moq;
using SolidTUS.Validators;

namespace SolidTUS.Tests.Mocks;

public static class MockValidators
{
    public static IChecksumValidator ChecksumValidator(string algorithmName, bool checksumIsValid)
    {
        var mock = new Mock<IChecksumValidator>();

        mock.Setup(v => v.AlgorithmName)
        .Returns(algorithmName);

        mock.Setup(v => v.ValidateChecksumAsync(It.IsAny<Stream>(), It.IsAny<byte[]>()))
        .ReturnsAsync(checksumIsValid);

        return mock.Object;
    }
}
