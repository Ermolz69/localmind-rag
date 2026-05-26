using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/diagnostics", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetAsync(cancellationToken), context))
            .WithName("Diagnostics")
            .WithTags("Diagnostics")
            .WithSummary("Gets local diagnostics.")
            .WithDescription("Returns runtime, storage, ingestion, and sync diagnostics for the local installation.")
            .Produces<ApiResponse<DiagnosticsDto>>();

        return app;
    }
}
