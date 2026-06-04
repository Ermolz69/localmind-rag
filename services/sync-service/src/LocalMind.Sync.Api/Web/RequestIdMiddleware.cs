namespace LocalMind.Sync.Api.Web;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";
    public const string ItemName = "RequestId";
    private readonly RequestDelegate next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string requestId = context.Request.Headers.TryGetValue(HeaderName, out Microsoft.Extensions.Primitives.StringValues value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemName] = requestId;
        context.Response.Headers[HeaderName] = requestId;
        await next(context);
    }
}
