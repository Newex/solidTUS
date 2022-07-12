using System;
using LanguageExt;
using SolidTUS.Models;

namespace SolidTUS.Tests.Tools;

public static class TestExtensions
{
    public static bool IsSuccess<L, R>(this Either<L, R> either) => either.Match(_ => true, _ => false);
    public static int StatusCode<R>(this Either<HttpError, R> either, int onSuccess = 200) => either.Match(_ => onSuccess, e => e.StatusCode);
}
