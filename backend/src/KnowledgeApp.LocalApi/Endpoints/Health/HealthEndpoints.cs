namespace KnowledgeApp.LocalApi.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "OK", app = "localmind" }))
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Checks LocalApi health.")
            .WithDescription("Returns a small readiness payload for the local desktop backend.")
            .Produces(StatusCodes.Status200OK);

        return app;
    }
}
