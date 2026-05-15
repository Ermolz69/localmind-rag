namespace KnowledgeApp.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string code, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(code, message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string code, string message)
        : base(code, message)
    {
    }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string code, string message)
        : base(code, message)
    {
    }
}

public sealed class UnsupportedFileAppException : AppException
{
    public UnsupportedFileAppException(string code, string message, Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}

public sealed class ExternalDependencyAppException : AppException
{
    public ExternalDependencyAppException(string code, string message, Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}
