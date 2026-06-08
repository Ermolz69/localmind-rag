namespace KnowledgeApp.LocalApi.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "OK",
            service = "KnowledgeApp.LocalApi",
            supervisorInstanceId = Environment.GetEnvironmentVariable("LOCALMIND_SUPERVISOR_TOKEN")
        }))
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Checks LocalApi health.")
            .WithDescription("Returns a small readiness payload for the local desktop backend.")
            .Produces(StatusCodes.Status200OK);

        return app;
    }
}
