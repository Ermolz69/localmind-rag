using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.WatchedFolders;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class WatchedFolderEndpoints
{
    public static IEndpointRouteBuilder MapWatchedFolderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/watched-folders/status", async (
            ISettingsService settingsService,
            IWatchedFolderStatusStore statusStore,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            Contracts.Settings.AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

            WatchedFolderStatusResponse status = statusStore.GetStatus(settings.WatchedFolders);

            return ApiResults.Ok(status, context);
        })
        .WithName("GetWatchedFolderStatus")
        .WithTags("WatchedFolders")
        .WithSummary("Gets watched folder status.")
        .WithDescription("Returns watched folder watcher state, pending event count, and sanitized watcher errors.")
        .Produces<ApiResponse<WatchedFolderStatusResponse>>();

        return app;
    }
}
