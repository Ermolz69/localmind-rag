using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/diagnostics", async (
                ILocalDiagnosticsService diagnostics,
                CancellationToken cancellationToken) =>
            Results.Ok(await diagnostics.GetAsync(cancellationToken)))
            .WithName("Diagnostics");

        return app;
    }
}
