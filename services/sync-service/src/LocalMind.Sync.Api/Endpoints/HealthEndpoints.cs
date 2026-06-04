namespace LocalMind.Sync.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => Results.Ok(new { status = "OK", app = "localmind-sync-service" }));
        return api;
    }
}
