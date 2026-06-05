namespace LocalMind.Sync.Api.Web;

using LocalMind.Sync.Application.Common;
using LocalMind.Sync.Contracts.Common;

public static class EndpointResults
{
    public static IResult From<T>(Result<T> result, HttpContext context, int successStatusCode = StatusCodes.Status200OK)
    {
        string requestId = RequestId(context);
        if (result.IsSuccess && result.Value is not null)
        {
            return Results.Json(ApiResponseDto<T>.Ok(result.Value, requestId), statusCode: successStatusCode);
        }

        ApplicationError error = result.Error ?? new ApplicationError("INTERNAL_SERVER_ERROR", "Unexpected sync service error");
        ApiErrorDto errorDto = new(error.Code, error.Message, error.Details ?? new Dictionary<string, string>());
        int statusCode = StatusCode(error.Code);
        return Results.Json(ApiResponseDto<T>.Fail(errorDto, requestId), statusCode: statusCode);
    }

    private static string RequestId(HttpContext context)
    {
        return context.Items.TryGetValue(RequestIdMiddleware.ItemName, out object? value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static int StatusCode(string code)
    {
        if (code.EndsWith("_NOT_FOUND", StringComparison.Ordinal))
        {
            return StatusCodes.Status404NotFound;
        }

        if (code is "IDEMPOTENCY_REPLAY" or "DEVICE_SYNC_LOCKED")
        {
            return StatusCodes.Status409Conflict;
        }

        if (code is "VALIDATION_FAILED")
        {
            return StatusCodes.Status400BadRequest;
        }

        return StatusCodes.Status500InternalServerError;
    }
}
