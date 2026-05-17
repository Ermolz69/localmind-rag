using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings", async (
                ISettingsService settings,
                CancellationToken cancellationToken) =>
            Results.Ok(await settings.GetAsync(cancellationToken)))
            .WithName("GetSettings");

        app.MapPut("/api/settings", async (
            AppSettingsDto request,
            ISettingsService settings,
            CancellationToken cancellationToken) =>
        {
            await settings.UpdateAsync(request, cancellationToken);
            return Results.NoContent();
        }).WithName("UpdateSettings");

        return app;
    }
}
