namespace LocalMind.Sync.Api.Endpoints;

using LocalMind.Sync.Api.Web;
using LocalMind.Sync.Application.Sync;
using LocalMind.Sync.Contracts.Sync;

public static class SyncEndpoints
{
    public static RouteGroupBuilder MapSyncEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/sync/push", async (
                PushRequest request,
                SyncService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.PushAsync(request, cancellationToken), context, StatusCodes.Status202Accepted));

        api.MapPost("/sync/pull", async (
                PullRequest request,
                SyncService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.PullAsync(request, cancellationToken), context));

        api.MapPost("/sync/manifest", async (
                SubmitManifestRequest request,
                SyncService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.SubmitManifestAsync(request, cancellationToken), context, StatusCodes.Status202Accepted));

        return api;
    }
}
