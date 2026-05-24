using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class RuntimeEndpoints
{
    public static IEndpointRouteBuilder MapRuntimeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/runtime/status", async (
                IAiRuntimeManager runtime,
                HttpContext context,
                CancellationToken cancellationToken) =>
            ApiResults.Ok(await runtime.GetStatusAsync(cancellationToken), context))
            .WithName("GetRuntimeStatus")
            .WithTags("Runtime")
            .WithSummary("Gets runtime status.")
            .WithDescription("Returns LocalApi readiness, AI runtime state, model availability, and setup guidance.")
            .Produces<ApiResponse<RuntimeStatusDto>>();

        app.MapPost("/api/runtime/ai/start", async (
            IAiRuntimeManager runtime,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await runtime.StartAsync(cancellationToken);
            return ApiResults.Accepted<object?>(null, null, context);
        })
            .WithName("StartAiRuntime")
            .WithTags("Runtime")
            .WithSummary("Starts the AI runtime.")
            .WithDescription("Starts the local AI runtime process when it is available.")
            .Produces<ApiResponse<object?>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/api/runtime/ai/setup", async (
            IAiRuntimeSetupService setup,
            IAiRuntimeManager runtime,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await setup.SetupAsync(cancellationToken);
            RuntimeStatusDto status = await runtime.GetStatusAsync(cancellationToken);
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

        app.MapGet("/api/runtime/models", async (
                IAiModelRegistry registry,
                HttpContext context,
                CancellationToken cancellationToken) =>
            ApiResults.Ok(await registry.ListModelsAsync(cancellationToken), context))
            .WithName("ListRuntimeModels")
            .WithTags("Runtime")
            .WithSummary("Lists local AI models.")
            .WithDescription("Returns the local model names discovered by the configured AI model registry.")
            .Produces<ApiResponse<IReadOnlyCollection<string>>>();

        return app;
    }
}
