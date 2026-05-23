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
        })
            .WithName("ProcessIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Processes an ingestion job.")
            .WithDescription("Runs ingestion for an existing job and returns the queued/accepted status.")
            .Produces<ProcessIngestionJobResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
