using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ingestion/jobs", async (
            string? status,
            int? limit,
            int? offset,
            [FromServices] ListIngestionJobsHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(
                new ListIngestionJobsQuery(status, limit ?? 50, offset ?? 0),
                cancellationToken)).ToApiResult(context))
            .WithName("ListIngestionJobs")
            .WithTags("Ingestion")
            .WithSummary("Lists ingestion jobs.")
            .WithDescription("Returns ingestion jobs for tracking, retry, cancellation, and diagnostics.")
            .Produces<ApiResponse<IngestionJobListResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/ingestion/jobs/{id:guid}", async (
            Guid id,
            [FromServices] GetIngestionJobHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context))
            .WithName("GetIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Gets an ingestion job.")
            .WithDescription("Returns ingestion job state, diagnostics, and available control actions.")
            .Produces<ApiResponse<IngestionJobDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/ingestion/jobs/{id:guid}/process", async (
            Guid id,
            [FromServices] ProcessIngestionJobHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken))
                .ToAcceptedApiResult(
                    context,
                    response => $"{ApiVersions.V1Prefix}/ingestion/jobs/{response.JobId}");
        })
            .WithName("ProcessIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Processes an ingestion job.")
            .WithDescription("Runs ingestion for an existing job and returns the queued/accepted status.")
            .Produces<ApiResponse<ProcessIngestionJobResponse>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        app.MapPost("/ingestion/jobs/{id:guid}/retry", async (
            Guid id,
            [FromServices] RetryIngestionJobHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken))
                .ToAcceptedApiResult(
                    context,
                    response => $"{ApiVersions.V1Prefix}/ingestion/jobs/{response.JobId}"))
            .WithName("RetryIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Retries an ingestion job.")
            .WithDescription("Moves a failed or cancelled ingestion job back to the queued state.")
            .Produces<ApiResponse<IngestionJobActionResponse>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        app.MapPost("/ingestion/jobs/{id:guid}/cancel", async (
            Guid id,
            [FromServices] CancelIngestionJobHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context))
            .WithName("CancelIngestionJob")
            .WithTags("Ingestion")
            .WithSummary("Cancels an ingestion job.")
            .WithDescription("Marks a queued or running ingestion job as cancelled.")
            .Produces<ApiResponse<IngestionJobActionResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        return app;
    }
}
