namespace LocalMind.Sync.Application.Common;

public sealed class Result<T>
{
    private Result(T? value, ApplicationError? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public ApplicationError? Error { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, null, true);
    }

    public static Result<T> Failure(ApplicationError error)
    {
        return new Result<T>(default, error, false);
    }
}
