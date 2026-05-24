using KnowledgeApp.Contracts.Common;

namespace KnowledgeApp.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    UnsupportedMedia,
    Unprocessable,
    ExternalDependency,
    NotImplemented,
    Unexpected,
}

public sealed record ApplicationError(
    ErrorType Type,
    string Code,
    string Message,
    IReadOnlyList<ApiErrorDetail>? Details = null);

public sealed class Result<T>
{
    private Result(T? value, ApplicationError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public T? Value { get; }

    public ApplicationError? Error { get; }

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(ApplicationError error) => new(default, error);

    public static Result<T> Failure(Result result)
    {
        return result.Error is null
            ? throw new ArgumentException("Cannot create a failed generic result from a successful result.", nameof(result))
            : Failure(result.Error);
    }
}

public sealed class Result
{
    private Result(ApplicationError? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public ApplicationError? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(ApplicationError error) => new(error);
}

public static class ApplicationErrors
{
    public static ApplicationError Validation(string code, string message, IReadOnlyList<ApiErrorDetail>? details = null)
    {
        return new ApplicationError(ErrorType.Validation, code, message, details);
    }

    public static ApplicationError Validation(string code, string message, IReadOnlyDictionary<string, string[]> errors)
    {
        ApiErrorDetail[] details = errors
            .SelectMany(error => error.Value.Select(message => new ApiErrorDetail(error.Key, message)))
            .ToArray();

        return Validation(code, message, details);
    }

    public static ApplicationError NotFound(string code, string message)
    {
        return new ApplicationError(ErrorType.NotFound, code, message);
    }

    public static ApplicationError Conflict(string code, string message)
    {
        return new ApplicationError(ErrorType.Conflict, code, message);
    }

    public static ApplicationError UnsupportedMedia(string code, string message, IReadOnlyList<ApiErrorDetail>? details = null)
    {
        return new ApplicationError(ErrorType.UnsupportedMedia, code, message, details);
    }

    public static ApplicationError ExternalDependency(string code, string message)
    {
        return new ApplicationError(ErrorType.ExternalDependency, code, message);
    }

    public static ApplicationError NotImplemented(string code, string message)
    {
        return new ApplicationError(ErrorType.NotImplemented, code, message);
    }

    public static ApplicationError Unexpected(string code, string message)
    {
        return new ApplicationError(ErrorType.Unexpected, code, message);
    }
}
