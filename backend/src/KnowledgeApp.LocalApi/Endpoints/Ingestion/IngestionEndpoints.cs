using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Ingestion;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingestion/jobs/{id:guid}/process", async (
            Guid id,
            ProcessIngestionJobHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            ProcessIngestionJobResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return ApiResults.Failure(ApplicationErrors.NotFound("INGESTION_JOB_NOT_FOUND", "Ingestion job was not found."), context);
            }

            return ApiResults.Accepted(
                $"/api/ingestion/jobs/{id}",
                new ProcessIngestionJobResponse(result.JobId!.Value, result.Status ?? "Queued"),
                context);
        })
            .WithName("ProcessIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Processes an ingestion job.")
            .WithDescription("Runs ingestion for an existing job and returns the queued/accepted status.")
            .Produces<ApiResponse<ProcessIngestionJobResponse>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }
}
