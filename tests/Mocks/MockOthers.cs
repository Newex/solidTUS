using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;

using Moq;

namespace SolidTus.Tests.Mocks;

public static class MockOthers
{
    public static ISystemClock Clock(DateTimeOffset now)
    {
        var mock = new Mock<ISystemClock>();

        mock.Setup(c => c.UtcNow)
        .Returns(now);

        return mock.Object;
    }
}
