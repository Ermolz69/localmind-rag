namespace KnowledgeApp.Contracts.Common;

/// <summary>Standard LocalApi response envelope.</summary>
/// <typeparam name="T">Response payload type.</typeparam>
/// <param name="Success">True when the operation completed successfully.</param>
/// <param name="Data">Response payload for successful operations.</param>
/// <param name="Error">Error information for failed operations.</param>
/// <param name="Metadata">Response metadata shared by success and error responses.</param>
public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    ApiError? Error,
    ApiMetadata Metadata);

/// <summary>Standard LocalApi error payload.</summary>
/// <param name="Code">Stable frontend-facing error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional field-level error details.</param>
public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyList<ApiErrorDetail>? Details = null);

/// <summary>Field-level API error detail.</summary>
/// <param name="Field">Field or parameter that failed validation.</param>
/// <param name="Message">Human-readable validation message.</param>
public sealed record ApiErrorDetail(string? Field, string Message);

/// <summary>Standard LocalApi response metadata.</summary>
/// <param name="Timestamp">UTC response timestamp.</param>
/// <param name="RequestId">ASP.NET Core request trace identifier.</param>
public sealed record ApiMetadata(DateTimeOffset Timestamp, string? RequestId = null);

/// <summary>Factory methods for standard LocalApi response envelopes.</summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? requestId)
    {
        return new ApiResponse<T>(
            Success: true,
            Data: data,
            Error: null,
            Metadata: CreateMetadata(requestId));
    }

    public static ApiResponse<object?> Failure(
        string code,
        string message,
        string? requestId,
        IReadOnlyList<ApiErrorDetail>? details = null)
    {
        return new ApiResponse<object?>(
            Success: false,
            Data: null,
            Error: new ApiError(code, message, details ?? []),
            Metadata: CreateMetadata(requestId));
    }

    private static ApiMetadata CreateMetadata(string? requestId)
    {
        return new ApiMetadata(DateTimeOffset.UtcNow, requestId);
    }
}
