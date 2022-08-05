using System;
using SolidTUS.Models;

namespace SolidTUS.Tests.Tools;

public static class TestExtensions
{
    public static bool IsSuccess<R>(this Result<R> either) => either.Match(_ => true, _ => false);
    public static int StatusCode<R>(this Result<R> either, int onSuccess = 200) => either.Match(_ => onSuccess, e => e.StatusCode);
}
