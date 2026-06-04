namespace LocalMind.Sync.Api.Endpoints;

using LocalMind.Sync.Api.Web;
using LocalMind.Sync.Application.Conflicts;
using LocalMind.Sync.Contracts.Conflicts;

public static class ConflictEndpoints
{
    public static RouteGroupBuilder MapConflictEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/sync/conflicts", async (
                ConflictService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.ListOpenAsync(cancellationToken), context));

        api.MapPost("/sync/conflicts/{conflictId:guid}/resolve", async (
                Guid conflictId,
                ResolveConflictRequest request,
                ConflictService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.ResolveAsync(conflictId, request, cancellationToken), context));

        return api;
    }
}
