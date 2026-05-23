using KnowledgeApp.Application.Abstractions;

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
            Guid operationId = diagnostics.BeginOperation("sync", "status");
            object status = await sync.GetStatusAsync(cancellationToken);
            diagnostics.LogStep(operationId, "status-returned");
            return Results.Ok(status);
        });

        app.MapPost("/api/sync/run", async (
            ISyncService sync,
            IAppDiagnosticLogger diagnostics,
            CancellationToken cancellationToken) =>
        {
            Guid operationId = diagnostics.BeginOperation("sync", "run");
            await sync.RunAsync(cancellationToken);
            diagnostics.LogStep(operationId, "run-accepted");
            return Results.Accepted();
        });

        app.MapPost("/api/sync/login", () =>
            Results.Problem(
                "Remote sync auth is not implemented in the skeleton.",
                statusCode: StatusCodes.Status501NotImplemented));

        app.MapPost("/api/sync/logout", () => Results.NoContent());

        return app;
    }
}
