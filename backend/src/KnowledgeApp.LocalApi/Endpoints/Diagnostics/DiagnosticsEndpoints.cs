using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/diagnostics")
            .WithTags("Diagnostics");

        group.MapGet("/", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetAsync(cancellationToken), context))
            .WithName("Diagnostics")
            .WithSummary("Gets local diagnostics.")
            .WithDescription("Returns runtime, storage, ingestion, and sync diagnostics for the local installation.")
            .Produces<ApiResponse<DiagnosticsDto>>();

        group.MapGet("/health", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetGeneralHealthAsync(cancellationToken), context))
            .WithName("DiagnosticsHealth")
            .WithSummary("Gets general diagnostics health.")
            .Produces<ApiResponse<DiagnosticsHealthStatus>>();

        group.MapGet("/database", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetDatabaseAsync(cancellationToken), context))
            .WithName("DiagnosticsDatabase")
            .WithSummary("Gets database diagnostics.")
            .Produces<ApiResponse<DiagnosticsDatabaseDto>>();

        group.MapGet("/storage", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetStorageAsync(cancellationToken), context))
            .WithName("DiagnosticsStorage")
            .WithSummary("Gets storage diagnostics.")
            .Produces<ApiResponse<DiagnosticsStorageDto>>();

        group.MapGet("/runtime", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetRuntimeAsync(cancellationToken), context))
            .WithName("DiagnosticsRuntime")
            .WithSummary("Gets runtime diagnostics.")
            .Produces<ApiResponse<DiagnosticsRuntimeDto>>();

        group.MapGet("/vector-index", async (
            ILocalDiagnosticsService diagnostics,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await diagnostics.GetVectorIndexAsync(cancellationToken), context))
            .WithName("DiagnosticsVectorIndex")
            .WithSummary("Gets vector index diagnostics.")
            .Produces<ApiResponse<DiagnosticsVectorIndexDto>>();

        group.MapGet("/operations", async (
            [Microsoft.AspNetCore.Http.AsParameters] CursorPageRequest request,
            MediatR.IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await mediator.Send(new KnowledgeApp.Application.Diagnostics.Queries.GetOperationLogsQuery(request.Limit, request.Cursor), cancellationToken), context))
            .WithName("DiagnosticsOperations")
            .WithSummary("Gets recent operation logs.")
            .Produces<ApiResponse<CursorPage<KnowledgeApp.Contracts.Diagnostics.OperationLogDto>>>();

        group.MapPost("/logs/cleanup", async (
            ILogMaintenanceService logMaintenance,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await logMaintenance.CleanupAllAsync(cancellationToken), context))
            .WithName("DiagnosticsLogsCleanup")
            .WithSummary("Deletes local log files.")
            .WithDescription("Removes LocalMind log files from the logs folder. Files currently in use are skipped.")
            .Produces<ApiResponse<LogCleanupResultDto>>();

        return app;
    }
}
