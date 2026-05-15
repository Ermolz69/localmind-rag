using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sync/status", async (ISyncService sync, CancellationToken cancellationToken) =>
            Results.Ok(await sync.GetStatusAsync(cancellationToken)));

        app.MapPost("/api/sync/run", async (ISyncService sync, CancellationToken cancellationToken) =>
        {
            await sync.RunAsync(cancellationToken);
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
