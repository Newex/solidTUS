using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;
using Moq;

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

    public static LinkGenerator LinkGenerator()
    {
        var mock = new Mock<LinkGenerator>();

        return mock.Object;
    }
}
