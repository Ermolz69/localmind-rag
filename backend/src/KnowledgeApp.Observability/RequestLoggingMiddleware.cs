using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Observability;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Exception? capturedException = null;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            capturedException = exception;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.Elapsed, capturedException);
        }
    }

    private void LogRequest(HttpContext context, TimeSpan elapsed, Exception? exception)
    {
        string endpoint = context.GetEndpoint()?.DisplayName ?? "Unmatched";
        int statusCode = context.Response.HasStarted
            ? context.Response.StatusCode
            : context.Response.StatusCode;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventKind"] = "HttpRequest",
            ["TraceId"] = context.TraceIdentifier,
            ["HttpMethod"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value,
            ["Endpoint"] = endpoint,
            ["StatusCode"] = statusCode,
            ["ElapsedMilliseconds"] = elapsed.TotalMilliseconds,
        });

        if (exception is not null)
        {
            logger.LogError(
                exception,
                "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMilliseconds:0.0} ms",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsed.TotalMilliseconds);
            return;
        }

        LogLevel level = statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : statusCode >= StatusCodes.Status400BadRequest
                ? LogLevel.Warning
                : LogLevel.Information;

        logger.Log(
            level,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0.0} ms",
            context.Request.Method,
            context.Request.Path.Value,
            statusCode,
            elapsed.TotalMilliseconds);
    }
}
