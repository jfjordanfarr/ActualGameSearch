namespace ActualGameSearch.Core.Models;

public sealed record Result<T>(bool Success, T? Data, string? Error = null, string? Code = null)
{
    public static Result<T> Ok(T data) => new(true, data);
    public static Result<T> Fail(string error, string? code = null) => new(false, default, error, code);
}

public static class Result
{
    public static Result<T> Ok<T>(T data) => Result<T>.Ok(data);
    public static Result<T> Fail<T>(string error, string? code = null) => Result<T>.Fail(error, code);
}