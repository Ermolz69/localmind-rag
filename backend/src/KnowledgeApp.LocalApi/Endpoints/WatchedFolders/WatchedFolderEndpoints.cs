using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;
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
            AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

            WatchedFoldersSettingsDto watchedFolders =
                settings.WatchedFolders ?? CreateDisabledWatchedFolderSettings();

            WatchedFolderStatusResponse status = statusStore.GetStatus(watchedFolders);

            return ApiResults.Ok(status, context);
        })
        .WithName("GetWatchedFolderStatus")
        .WithTags("WatchedFolders")
        .WithSummary("Gets watched folder status.")
        .WithDescription("Returns watched folder watcher state, pending event count, and sanitized watcher errors.")
        .Produces<ApiResponse<WatchedFolderStatusResponse>>();

        return app;
    }

    private static WatchedFoldersSettingsDto CreateDisabledWatchedFolderSettings()
    {
        return new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);
    }
}
