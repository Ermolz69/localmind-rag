using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Commands;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Queries;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class WatchedFolderEndpoints
{
    public static IEndpointRouteBuilder MapWatchedFolderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/watched-folders/status", async (
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var status = await sender.Send(new GetWatchedFolderStatusQuery(), cancellationToken);
            return ApiResults.Ok(status, context);
        })
        .WithName("GetWatchedFolderStatus")
        .WithTags("WatchedFolders")
        .WithSummary("Gets watched folder status.")
        .WithDescription("Returns watched folder watcher state, pending event count, and sanitized watcher errors.")
        .Produces<ApiResponse<WatchedFolderStatusResponse>>();

        app.MapPost("/watched-folders/rescan", async (
            [FromBody] RescanWatchedFoldersRequest request,
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var command = new RescanWatchedFoldersCommand(request.Path);
            var result = await sender.Send(command, cancellationToken);
            return result.ToApiResult(context);
        })
        .WithName("RescanWatchedFolders")
        .WithTags("WatchedFolders")
        .WithSummary("Rescan watched folders.")
        .WithDescription("Manually rescans watched folders to find missed changes.")
        .Produces<ApiResponse<RescanWatchedFoldersResponse>>();

        app.MapPost("/watched-folders/cleanup", async (
            [FromServices] CleanupWatchedFoldersHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new CleanupWatchedFoldersCommand(), cancellationToken);
            return result.ToApiResult(context);
        })
        .WithName("CleanupWatchedFolders")
        .WithTags("WatchedFolders")
        .WithSummary("Clean up deleted watched files.")
        .WithDescription("Removes internal application data for watched files that have been marked as deleted.")
        .Produces<ApiResponse<WatchedFolderCleanupResponse>>();

        return app;
    }

}
