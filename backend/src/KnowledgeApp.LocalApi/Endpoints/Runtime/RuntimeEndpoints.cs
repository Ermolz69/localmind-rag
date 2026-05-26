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

        app.MapPost("/runtime/ai/setup", async (
            [FromServices] IAiRuntimeSetupService setup,
            [FromServices] IAiRuntimeProviderRegistry providers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await setup.SetupAsync(cancellationToken);

            RuntimeStatusDto status = await providers.GetSelectedProvider().GetStatusAsync(cancellationToken);

            return ApiResults.Ok(new RuntimeSetupResponse(
                RuntimeInstalled: status.AiRuntimeStatus != "RuntimeMissing",
                ModelInstalled: status.ModelsAvailable,
                Message: status.SetupRequired
                    ? status.SetupReason ?? "AI setup is incomplete."
                    : "Local AI runtime setup completed.",
                Status: status),
                context);
        })
            .WithName("SetupAiRuntime")
            .WithTags("Runtime")
            .WithSummary("Sets up the AI runtime.")
            .WithDescription("Downloads or prepares local AI runtime assets and returns the resulting runtime status.")
            .Produces<ApiResponse<RuntimeSetupResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

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
