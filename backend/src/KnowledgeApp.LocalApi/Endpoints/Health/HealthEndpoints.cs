namespace KnowledgeApp.LocalApi.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "OK", app = "localmind" }))
            .WithName("Health");

        return app;
    }
}
