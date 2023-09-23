using System;
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
}
