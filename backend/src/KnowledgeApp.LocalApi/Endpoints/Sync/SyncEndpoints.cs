using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sync/status", async (
            ISyncService sync,
            IAppDiagnosticLogger diagnostics,
            CancellationToken cancellationToken) =>
        {
            Guid operationId = diagnostics.BeginOperation(DiagnosticNames.Areas.Sync, DiagnosticNames.Operations.SyncStatus);
            SyncStatusDto status = await sync.GetStatusAsync(cancellationToken);
            diagnostics.LogStep(operationId, DiagnosticNames.Steps.StatusReturned);
            return Results.Ok(status);
        })
            .WithName("GetSyncStatus")
            .WithTags("Sync")
            .WithSummary("Gets sync status.")
            .WithDescription("Returns current remote sync availability and pending operation count.")
            .Produces<SyncStatusDto>();

        app.MapPost("/api/sync/run", async (
            ISyncService sync,
            IAppDiagnosticLogger diagnostics,
            CancellationToken cancellationToken) =>
        {
            Guid operationId = diagnostics.BeginOperation(DiagnosticNames.Areas.Sync, DiagnosticNames.Operations.SyncRun);
            await sync.RunAsync(cancellationToken);
            diagnostics.LogStep(operationId, DiagnosticNames.Steps.RunAccepted);
            return Results.Accepted();
        })
            .WithName("RunSync")
            .WithTags("Sync")
            .WithSummary("Runs sync.")
            .WithDescription("Starts a local sync attempt when remote sync is configured and online.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/api/sync/login", () =>
            Results.Problem(
                "Remote sync auth is not implemented in the skeleton.",
                statusCode: StatusCodes.Status501NotImplemented))
            .WithName("LoginSync")
            .WithTags("Sync")
            .WithSummary("Starts sync login.")
            .WithDescription("Placeholder for future remote sync authentication.")
            .ProducesProblem(StatusCodes.Status501NotImplemented);

        app.MapPost("/api/sync/logout", () => Results.NoContent())
            .WithName("LogoutSync")
            .WithTags("Sync")
            .WithSummary("Logs out of sync.")
            .WithDescription("Clears future remote sync session state.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
