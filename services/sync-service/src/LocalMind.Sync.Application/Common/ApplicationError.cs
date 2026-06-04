namespace LocalMind.Sync.Application.Common;

public sealed record ApplicationError(string Code, string Message, IReadOnlyDictionary<string, string>? Details = null)
{
    public static ApplicationError Validation(string message, IReadOnlyDictionary<string, string>? details = null)
    {
        return new ApplicationError("VALIDATION_FAILED", message, details);
    }

    public static ApplicationError NotFound(string code, string message)
    {
        return new ApplicationError(code, message);
    }

    public static ApplicationError Conflict(string code, string message)
    {
        return new ApplicationError(code, message);
    }
}
