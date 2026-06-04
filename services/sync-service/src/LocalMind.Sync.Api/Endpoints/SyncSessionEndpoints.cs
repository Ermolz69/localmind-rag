namespace LocalMind.Sync.Api.Endpoints;

using LocalMind.Sync.Api.Web;
using LocalMind.Sync.Application.Sessions;
using LocalMind.Sync.Contracts.Sessions;

public static class SyncSessionEndpoints
{
    public static RouteGroupBuilder MapSyncSessionEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/sync/sessions", async (
                CreateSyncSessionRequest request,
                SyncSessionService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.CreateAsync(request, cancellationToken), context, StatusCodes.Status201Created));

        api.MapGet("/sync/sessions/{sessionId:guid}", async (
                Guid sessionId,
                SyncSessionService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.GetAsync(sessionId, cancellationToken), context));

        return api;
    }
}
