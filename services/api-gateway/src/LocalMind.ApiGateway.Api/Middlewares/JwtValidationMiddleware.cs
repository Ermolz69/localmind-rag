using System.Threading.Tasks;
using LocalMind.ApiGateway.Application.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LocalMind.ApiGateway.Api.Middlewares;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IValidateTokenUseCase validateTokenUseCase)
    {
        // Skip validation for auth-service routes
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("Missing Authorization header.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing Authorization header." });
            return;
        }

        try
        {
            var claims = await validateTokenUseCase.ExecuteAsync(authHeader, context.RequestAborted);
            if (claims != null)
            {
                // Inject X-User-Id header for downstream services
                context.Request.Headers["X-User-Id"] = claims.UserId;
                
                if (!string.IsNullOrEmpty(claims.Role))
                {
                    context.Request.Headers["X-User-Role"] = claims.Role;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token." });
                return;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Token validation failed.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token validation failed." });
            return;
        }

        await _next(context);
    }
}
