using SolidTUS.Models;
using SolidTUS.Models.Functional;

namespace SolidTUS.Tests.Tools;

internal static class TestExtensions
{
    public static bool IsSuccess<R> (this Result<R, HttpError> either) where R : notnull => either.MatchValue(s => s is not null, _ => false);
    public static int StatusCode<R>(this Result<R, HttpError> either, int onSuccess = 200) where R : notnull => either.MatchValue(_ => onSuccess, e => e.StatusCode);
}
