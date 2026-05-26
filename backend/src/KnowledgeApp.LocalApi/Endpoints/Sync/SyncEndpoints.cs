using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sync/status", async (
            ISyncService sync,
            IAppDiagnosticLogger diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            Guid operationId = diagnostics.BeginOperation(
                DiagnosticNames.Areas.Sync,
                DiagnosticNames.Operations.SyncStatus);

            SyncStatusDto status = await sync.GetStatusAsync(cancellationToken);

            diagnostics.LogStep(operationId, DiagnosticNames.Steps.StatusReturned);

            return ApiResults.Ok(status, context);
        })
            .WithName("GetSyncStatus")
            .WithTags("Sync")
            .WithSummary("Gets sync status.")
            .WithDescription("Returns current remote sync availability and pending operation count.")
            .Produces<ApiResponse<SyncStatusDto>>();

        app.MapPost("/sync/run", async (
            ISyncService sync,
            IAppDiagnosticLogger diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            Guid operationId = diagnostics.BeginOperation(
                DiagnosticNames.Areas.Sync,
                DiagnosticNames.Operations.SyncRun);

            await sync.RunAsync(cancellationToken);

            diagnostics.LogStep(operationId, DiagnosticNames.Steps.RunAccepted);

            return ApiResults.Accepted<object?>(null, null, context);
        })
            .WithName("RunSync")
            .WithTags("Sync")
            .WithSummary("Runs sync.")
            .WithDescription("Starts a local sync attempt when remote sync is configured and online.")
            .Produces<ApiResponse<object?>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/sync/login", (HttpContext context) =>
            ApiResults.Failure(
                ApplicationErrors.NotImplemented(
                    "SYNC_AUTH_NOT_IMPLEMENTED",
                    "Remote sync auth is not implemented in the skeleton."),
                context))
            .WithName("LoginSync")
            .WithTags("Sync")
            .WithSummary("Starts sync login.")
            .WithDescription("Placeholder for future remote sync authentication.")
            .Produces<ApiResponse<object?>>(StatusCodes.Status501NotImplemented);

        app.MapPost("/sync/logout", (HttpContext context) => ApiResults.Empty(context))
            .WithName("LogoutSync")
            .WithTags("Sync")
            .WithSummary("Logs out of sync.")
            .WithDescription("Clears future remote sync session state.")
            .Produces<ApiResponse<object?>>();

        return app;
    }
}
