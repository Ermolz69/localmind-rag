using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings", async (
                ISettingsService settings,
                HttpContext context,
                CancellationToken cancellationToken) =>
            ApiResults.Ok(await settings.GetAsync(cancellationToken), context))
            .WithName("GetSettings")
            .WithTags("Settings")
            .WithSummary("Gets application settings.")
            .WithDescription("Returns local appearance, AI, runtime path, and sync settings.")
            .Produces<ApiResponse<AppSettingsDto>>();

        app.MapPut("/api/settings", async (
            AppSettingsDto request,
            ISettingsService settings,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await settings.UpdateAsync(request, cancellationToken);
            return ApiResults.Empty(context);
        })
            .WithName("UpdateSettings")
            .WithTags("Settings")
            .WithSummary("Updates application settings.")
            .WithDescription("Persists local appearance, AI, runtime path, and sync settings.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        return app;
    }
}
