using System;
using System.Threading;
using Microsoft.Extensions.Options;
using SolidTus.Tests.Mocks;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;

namespace SolidTus.Tests.ProtocolHandlerTests.ExpirationTests;

[UnitTest]
public class ExpirationTests
{
    [Fact]
    public void Setting_the_expiration_header_should_follow_RFC7231()
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );

        // 1st of June 2020 is a Monday
        var now = new DateTimeOffset(2020, 06, 01, 12, 30, 0, TimeSpan.FromHours(0));
        var request = RequestContext.Create(http, CancellationToken.None).Map(c =>
        {
            return c with
            {
                UploadFileInfo = new()
                {
                    CreatedDate = now
                }
            };
        });
        var clock = MockOthers.Clock(now);
        var options = Options.Create(new TusOptions
        {
            AbsoluteInterval = TimeSpan.FromMinutes(2),
            ExpirationStrategy = ExpirationStrategy.AbsoluteExpiration
        });
        var handler = new ExpirationRequestHandler(clock, options);

        // Act
        var response = request.Map(handler.SetExpiration).GetTusHttpResponse();
        var result = response.Headers[TusHeaderNames.Expiration];

        // Assert
        var expected = "Mon, 01 Jun 2020 12:32:00 GMT";
        Assert.Equal(expected, result);
    }
}
