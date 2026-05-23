using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/diagnostics", async (
                ILocalDiagnosticsService diagnostics,
                CancellationToken cancellationToken) =>
            Results.Ok(await diagnostics.GetAsync(cancellationToken)))
            .WithName("Diagnostics")
            .WithTags("Diagnostics")
            .WithSummary("Gets local diagnostics.")
            .WithDescription("Returns runtime, storage, ingestion, and sync diagnostics for the local installation.")
            .Produces<DiagnosticsDto>();

        return app;
    }
}
