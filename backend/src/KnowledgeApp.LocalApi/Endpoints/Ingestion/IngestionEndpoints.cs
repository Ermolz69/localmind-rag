using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Contracts.Ingestion;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingestion/jobs/{id:guid}/process", async (
            Guid id,
            ProcessIngestionJobHandler handler,
            CancellationToken cancellationToken) =>
        {
            ProcessIngestionJobResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return Results.NotFound();
            }

            return Results.Accepted(
                $"/api/ingestion/jobs/{id}",
                new ProcessIngestionJobResponse(result.JobId!.Value, result.Status ?? "Queued"));
        });

        return app;
    }
}
