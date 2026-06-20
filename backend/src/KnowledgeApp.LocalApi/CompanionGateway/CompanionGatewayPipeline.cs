using KnowledgeApp.Application.Companion;
using KnowledgeApp.Contracts.Companion;
using Microsoft.Extensions.FileProviders;

namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>
/// Configures the LAN gateway request pipeline: serve the SPA, authenticate API
/// requests by device token, enforce the per-device permission allowlist, then
/// forward to the loopback LocalApi. Extracted so it can be hosted live and in tests.
/// </summary>
public static class CompanionGatewayPipeline
{
    public static void Configure(
        WebApplication app,
        ICompanionPairingService pairing,
        ICompanionForwarder forwarder,
        IFileProvider? spaFiles)
    {
        if (spaFiles is not null)
        {
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = spaFiles });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = spaFiles });
        }

        app.Use(async (context, next) =>
        {
            PathString path = context.Request.Path;

            if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            CompanionRouteDecision decision = CompanionRoutePolicy.Evaluate(
                path.Value ?? string.Empty,
                context.Request.Method);

            if (decision.Access == CompanionRouteAccess.Blocked)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (decision.Access == CompanionRouteAccess.RequiresCapability)
            {
                string? token = ExtractBearerToken(context.Request);
                CompanionDeviceDto? device = token is null
                    ? null
                    : await pairing.FindByTokenAsync(token, context.RequestAborted);

                if (device is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                if (!CompanionRoutePolicy.HasCapability(device.Permissions, decision.Capability))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            await forwarder.ForwardAsync(context, context.RequestAborted);
        });

        if (spaFiles is not null)
        {
            app.MapFallback(async context =>
            {
                IFileInfo index = spaFiles.GetFileInfo("index.html");

                if (!index.Exists)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(index, context.RequestAborted);
            });
        }
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        string header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string token = header[prefix.Length..].Trim();
            return token.Length == 0 ? null : token;
        }

        return null;
    }
}
