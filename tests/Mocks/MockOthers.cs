using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;
using Moq;

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

        mock.Setup(x => x.GetPathByName(It.IsAny<string>(), It.IsAny<object?>()))
        .Returns(url);

        return mock.Object;
    }
}
