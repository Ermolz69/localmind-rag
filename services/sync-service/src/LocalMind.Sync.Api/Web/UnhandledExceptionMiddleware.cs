namespace LocalMind.Sync.Api.Web;

using LocalMind.Sync.Contracts.Common;

public sealed class UnhandledExceptionMiddleware
{
    private readonly ILogger<UnhandledExceptionMiddleware> logger;
    private readonly RequestDelegate next;

    public UnhandledExceptionMiddleware(RequestDelegate next, ILogger<UnhandledExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled sync service error");
            string requestId = context.Items.TryGetValue(RequestIdMiddleware.ItemName, out object? value) ? value?.ToString() ?? string.Empty : string.Empty;
            ApiErrorDto error = new("INTERNAL_SERVER_ERROR", "Unexpected sync service error", new Dictionary<string, string>());
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiResponseDto<object>.Fail(error, requestId));
        }
    }
}
