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
    [Theory]
    [InlineData(2020, 06, 01, 12, 30, 00, "Mon", "Jun")]
    [InlineData(2014, 06, 25, 16, 00, 00, "Wed", "Jun")]
    public void Setting_the_expiration_header_should_follow_RFC7231(int year, int month, int day, int hour, int minutes, int seconds, string dayName, string monthName)
    {
        // Arrange
        var http = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );

        // 1st of June 2020 is a Monday
        var now = new DateTimeOffset(year, month, day, hour, minutes, seconds, TimeSpan.FromHours(0));
        var request = RequestContext.Create(http, CancellationToken.None).Map(c => c with
        {
            UploadFileInfo = new()
            {
                CreatedDate = now
            }

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
        var expected = $"{dayName}, {day:00} {monthName} {year:0000} {hour:00}:{minutes+2:00}:{seconds:00} GMT";
        Assert.Equal(expected, result);
    }
}
