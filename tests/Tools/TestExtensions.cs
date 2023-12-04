using CSharpFunctionalExtensions;
using SolidTUS.Models;

namespace SolidTUS.Tests.Tools;

public static class TestExtensions
{
    public static bool IsSuccess<R>(this Result<R, HttpError> either) => either.Match(s => s is not null, _ => false);
    public static int StatusCode<R>(this Result<R, HttpError> either, int onSuccess = 200) => either.Match(_ => onSuccess, e => e.StatusCode);
}
