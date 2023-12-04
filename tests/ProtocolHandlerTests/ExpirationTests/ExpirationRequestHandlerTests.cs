using System;
using System.Threading;
using MSOptions = Microsoft.Extensions.Options.Options;
using SolidTUS.Constants;
using SolidTUS.Extensions;
using SolidTUS.Models;
using SolidTUS.Options;
using SolidTUS.ProtocolHandlers.ProtocolExtensions;
using SolidTUS.Tests.Mocks;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace SolidTUS.Tests.ProtocolHandlerTests.ExpirationTests;

[UnitTest]
public class ExpirationTests
{
    [Theory]
    [InlineData(2020, 06, 01, 12, 30, 00, "Mon", "Jun")]
    [InlineData(2014, 06, 25, 16, 00, 00, "Wed", "Jun")]
    public void Setting_the_expiration_header_should_follow_RFC7231(int year, int month, int day, int hour, int minutes, int seconds, string dayName, string monthName)
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );

        // 1st of June 2020 is a Monday
        var now = new DateTimeOffset(year, month, day, hour, minutes, seconds, TimeSpan.FromHours(0));
        var context = TusResult
            .Create(request, MockHttps.HttpResponse())
            .Map(c => c with
            {
                UploadFileInfo = new()
                {
                    CreatedDate = now,
                    ExpirationDate = now.AddMinutes(2)
                }

            });
        var clock = MockOthers.Clock(now);
        var globalOptions = MSOptions.Create(new TusOptions
        {
            ExpirationStrategy = ExpirationStrategy.AbsoluteExpiration
        });
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var handler = new ExpirationRequestHandler(clock, expiredHandler, globalOptions);

        // Act
        var response = context.Map(handler.SetExpiration).Value;
        var result = response?.ResponseHeaders[TusHeaderNames.Expiration];

        // Assert
        var expected = $"{dayName}, {day:00} {monthName} {year:0000} {hour:00}:{minutes+2:00}:{seconds:00} GMT";
        result.ToString().Should().Be(expected);
    }

    [Fact]
    public void Never_expires_should_not_have_expiration_header()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );

        // 1st of June 2020 is a Monday
        var now = new DateTimeOffset(2020, 06, 01, 12, 00, 00, TimeSpan.FromHours(0));
        var context = TusResult
            .Create(request, MockHttps.HttpResponse())
            .Map(c => c with
            {
                UploadFileInfo = new()
                {
                    CreatedDate = now,
                    ExpirationDate = null
                }
            });
        var clock = MockOthers.Clock(now);
        var globalOptions = MSOptions.Create(new TusOptions
        {
            ExpirationStrategy = ExpirationStrategy.Never
        });
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var handler = new ExpirationRequestHandler(clock, expiredHandler, globalOptions);

        // Act
        var response = context.Map(handler.SetExpiration).Value;
        var result = response?.ResponseHeaders[TusHeaderNames.Expiration];

        // Assert
        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Expired_upload_should_return_410_Gone()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );
        var response = MockHttps.HttpResponse();

        // 1st of June 2020 is a Monday
        var lastWeek = new DateTimeOffset(2020, 06, 01, 12, 00, 00, TimeSpan.FromHours(0));
        var today = lastWeek.AddDays(6);
        var yesterday = lastWeek.AddDays(5);

        var expiration = TusResult
            .Create(request, response)
            .Map(c => c with
            {
                UploadFileInfo = new()
                {
                    ExpirationDate = yesterday
                }
            });
        var clock = MockOthers.Clock(today);
        var globalOptions = MSOptions.Create(new TusOptions());
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var handler = new ExpirationRequestHandler(clock, expiredHandler, globalOptions);

        // Act
        var hasExpired = await expiration.Bind(async c => await handler.CheckExpirationAsync(c, CancellationToken.None));
        hasExpired.TryGetError(out var result);

        // Assert
        result.Should().NotBeNull().And.Match<HttpError>(x => x.StatusCode == 410);
    }

    [Fact]
    public async Task Expired_upload_can_be_allowed()
    {
        // Arrange
        var request = MockHttps.HttpRequest("PATCH",
            (TusHeaderNames.Resumable, TusHeaderValues.TusPreferredVersion)
        );

        // 1st of June 2020 is a Monday
        var lastWeek = new DateTimeOffset(2020, 06, 01, 12, 00, 00, TimeSpan.FromHours(0));
        var today = lastWeek.AddDays(7);
        var yesterday = today.AddDays(-1);

        var context = TusResult
            .Create(request, MockHttps.HttpResponse())
            .Map(c => c with
            {
                UploadFileInfo = new()
                {
                    CreatedDate = lastWeek,
                    ExpirationDate = yesterday
                }
            });
        var clock = MockOthers.Clock(today);
        var options = MSOptions.Create(new TusOptions
        {
            ExpirationStrategy = ExpirationStrategy.AbsoluteExpiration,
            AllowExpiredUploadsToContinue = true
        });
        var expiredHandler = MockHandlers.ExpiredUploadHandler();
        var handler = new ExpirationRequestHandler(clock, expiredHandler, options);

        // Act
        var response = await context.Bind(async c => await handler.CheckExpirationAsync(c, CancellationToken.None));
        var result = response.Value;

        // Assert
        result.Should().NotBeNull();
    }
}
