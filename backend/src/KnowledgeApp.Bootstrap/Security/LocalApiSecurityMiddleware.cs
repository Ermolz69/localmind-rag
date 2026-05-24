using System.Net;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Bootstrap.Security;

public sealed class LocalApiSecurityMiddleware(RequestDelegate next, IOptions<LocalApiSecurityOptions> options)
{
    private const string TokenHeaderName = "X-LocalMind-Token";
    private readonly LocalApiSecurityOptions options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (options.RequireLoopback && !IsLoopbackRequest(context))
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status403Forbidden,
                ErrorCodes.Security.LocalAccessDenied,
                ErrorMessages.Security.LocalAccessDenied);
            return;
        }

        if (context.Request.ContentLength > options.MaxRequestBodyBytes)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                ErrorCodes.Security.RequestTooLarge,
                ErrorMessages.Security.RequestTooLarge);
            return;
        }

        if (RequiresJsonContentType(context.Request) && !IsJsonContentType(context.Request.ContentType))
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status415UnsupportedMediaType,
                ErrorCodes.Security.UnsupportedMediaType,
                ErrorMessages.Security.UnsupportedMediaType);
            return;
        }

        if (RequiresToken(context.Request) && !string.IsNullOrWhiteSpace(options.Token))
        {
            if (!context.Request.Headers.TryGetValue(TokenHeaderName, out Microsoft.Extensions.Primitives.StringValues token))
            {
                await WriteFailureAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    ErrorCodes.Security.LocalTokenRequired,
                    ErrorMessages.Security.LocalTokenRequired);
                return;
            }

            if (!string.Equals(token.ToString(), options.Token, StringComparison.Ordinal))
            {
                await WriteFailureAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    ErrorCodes.Security.LocalTokenInvalid,
                    ErrorMessages.Security.LocalTokenInvalid);
                return;
            }
        }

        await next(context);
    }

    private static bool IsExempt(PathString path)
    {
        return path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/docs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresToken(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsDelete(request.Method)
            || HttpMethods.IsPatch(request.Method);
    }

    private static bool RequiresJsonContentType(HttpRequest request)
    {
        return (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsPatch(request.Method))
            && request.ContentLength is > 0
            && !request.Path.StartsWithSegments("/api/documents/upload", StringComparison.OrdinalIgnoreCase)
            && !request.HasFormContentType;
    }

    private static bool IsJsonContentType(string? contentType)
    {
        return contentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsLoopbackRequest(HttpContext context)
    {
        string host = context.Request.Host.Host;
        bool loopbackHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "tauri.localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddressLoopback(host);
        bool loopbackRemote = context.Connection.RemoteIpAddress is null
            || IPAddress.IsLoopback(context.Connection.RemoteIpAddress);

        return loopbackHost && loopbackRemote;
    }

    private static bool IPAddressLoopback(string host)
    {
        return IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address);
    }

    private static async Task WriteFailureAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Failure(code, message, context.TraceIdentifier));
    }
}
