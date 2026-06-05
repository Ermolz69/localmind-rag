namespace LocalMind.Sync.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSyncServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder api = endpoints.MapGroup("/api/v1");
        api.MapHealthEndpoints();
        api.MapDeviceEndpoints();
        api.MapSyncSessionEndpoints();
        api.MapSyncEndpoints();
        api.MapConflictEndpoints();
        return endpoints;
    }
}
