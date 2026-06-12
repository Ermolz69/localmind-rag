using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class RuntimeEndpoints
{
    public static IEndpointRouteBuilder MapRuntimeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/runtime/status", async (
            [FromServices] IAiRuntimeProviderRegistry providers,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await providers.GetSelectedProvider().GetStatusAsync(cancellationToken), context))
            .WithName("GetRuntimeStatus")
            .WithTags("Runtime")
            .WithSummary("Gets runtime status.")
            .WithDescription("Returns LocalApi readiness, AI runtime state, model availability, and setup guidance.")
            .Produces<ApiResponse<RuntimeStatusDto>>();

        app.MapPost("/runtime/ai/start", async (
            [FromServices] IAiRuntimeProviderRegistry providers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await providers.GetSelectedProvider().StartAsync(cancellationToken);

            return ApiResults.Accepted<object?>(null, null, context);
        })
            .WithName("StartAiRuntime")
            .WithTags("Runtime")
            .WithSummary("Starts the AI runtime.")
            .WithDescription("Starts the local AI runtime process when it is available.")
            .Produces<ApiResponse<object?>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/runtime/ai/setup", (
            [FromServices] IAiRuntimeSetupCoordinator coordinator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            RuntimeSetupStartedResponse response = coordinator.StartSetup(cancellationToken);

            return ApiResults.Ok(response, context);
        })
            .WithName("StartAiRuntimeSetup")
            .WithTags("Runtime")
            .WithSummary("Starts the AI runtime setup.")
            .WithDescription("Starts a background task to download or prepare local AI runtime assets.")
            .Produces<ApiResponse<RuntimeSetupStartedResponse>>();

        app.MapGet("/runtime/ai/setup/{setupId}/events", async (
            Guid setupId,
            [FromServices] IAiRuntimeSetupCoordinator coordinator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            await foreach (RuntimeSetupProgress progress in coordinator.WatchProgressAsync(setupId, cancellationToken))
            {
                string data = System.Text.Json.JsonSerializer.Serialize(progress);
                string eventName = progress.IsCompleted ? "completed" : (progress.IsFailed ? "failed" : "progress");
                
                await context.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
                await context.Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        })
            .WithName("WatchAiRuntimeSetup")
            .WithTags("Runtime")
            .WithSummary("Watches AI runtime setup progress.")
            .WithDescription("Streams Server-Sent Events (SSE) representing the progress of the AI runtime setup.")
            // Using raw SSE, no ApiResponse wrapper
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/runtime/models", async (
            [FromServices] IAiRuntimeProviderRegistry providers,
            HttpContext context,
            CancellationToken cancellationToken) =>
            ApiResults.Ok(await providers.GetSelectedProvider().ListModelsAsync(cancellationToken), context))
            .WithName("ListRuntimeModels")
            .WithTags("Runtime")
            .WithSummary("Lists local AI models.")
            .WithDescription("Returns the local model names discovered by the configured AI model registry.")
            .Produces<ApiResponse<IReadOnlyCollection<string>>>();

        app.MapGet("/runtime/providers", async (
            [FromServices] IAiRuntimeProviderRegistry providers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            IAiRuntimeProvider selected = providers.GetSelectedProvider();
            List<RuntimeProviderDto> providerDtos = new();

            foreach (IAiRuntimeProvider provider in providers.Providers)
            {
                RuntimeStatusDto status = await provider.GetStatusAsync(cancellationToken);

                providerDtos.Add(new RuntimeProviderDto(
                    Id: provider.ProviderId,
                    Name: provider.ProviderName,
                    Selected: string.Equals(provider.ProviderId, selected.ProviderId, StringComparison.OrdinalIgnoreCase),
                    Status: status.ProviderStatus,
                    Capabilities: provider.Capabilities,
                    BaseUrl: status.BaseUrl,
                    FailureReason: status.FailureReason));
            }

            return ApiResults.Ok(new RuntimeProviderListResponse(selected.ProviderId, providerDtos), context);
        })
            .WithName("ListRuntimeProviders")
            .WithTags("Runtime")
            .WithSummary("Lists AI runtime providers.")
            .WithDescription("Returns providers known to LocalApi with selected-provider, status, and capability metadata.")
            .Produces<ApiResponse<RuntimeProviderListResponse>>();

        return app;
    }
}
