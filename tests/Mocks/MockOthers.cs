using System;
using Microsoft.Extensions.Internal;
using Moq;
using SolidTUS.Models;
using SolidTUS.Tests.Fakes;

using SolidTUS.Wrappers;

namespace SolidTUS.Tests.Mocks;

public static class MockOthers
{
    public static ISystemClock Clock(DateTimeOffset? now = null)
    {
        var mock = new Mock<ISystemClock>();

        mock.Setup(c => c.UtcNow)
        .Returns(now ?? DateTimeOffset.UtcNow);

        return mock.Object;
    }

    public static ILinkGeneratorWrapper LinkGenerator(string? url = null)
    {
        var mock = new Mock<ILinkGeneratorWrapper>();

        mock.Setup(x => x.GetPathByName(It.IsAny<string>(), It.IsAny<(string, object)>(), It.IsAny<(string, object)[]>()))
        .Returns(url);

        return mock.Object;
    }

    internal static TusResult TusResult(UploadFileInfo info)
    {
        var mock = new Mock<TusResult>();
        // mock.SetupProperty(t => t.UploadFileInfo).SetReturnsDefault(RandomEntities.UploadFileInfo());
        mock.SetupGet(t => t.UploadFileInfo).Returns(info);
        return mock.Object;
    }
}
