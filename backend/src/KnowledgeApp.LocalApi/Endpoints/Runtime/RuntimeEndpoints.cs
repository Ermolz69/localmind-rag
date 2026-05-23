using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class RuntimeEndpoints
{
    public static IEndpointRouteBuilder MapRuntimeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/runtime/status", async (
                IAiRuntimeManager runtime,
                CancellationToken cancellationToken) =>
            Results.Ok(await runtime.GetStatusAsync(cancellationToken)));

        app.MapPost("/api/runtime/ai/start", async (
            IAiRuntimeManager runtime,
            CancellationToken cancellationToken) =>
        {
            await runtime.StartAsync(cancellationToken);
            return Results.Accepted();
        });

        app.MapPost("/api/runtime/ai/setup", async (
            IAiRuntimeSetupService setup,
            IAiRuntimeManager runtime,
            CancellationToken cancellationToken) =>
        {
            await setup.SetupAsync(cancellationToken);
            RuntimeStatusDto status = await runtime.GetStatusAsync(cancellationToken);
            return Results.Ok(new RuntimeSetupResponse(
                RuntimeInstalled: status.AiRuntimeStatus != "RuntimeMissing",
                ModelInstalled: status.ModelsAvailable,
                Message: status.SetupRequired
                    ? status.SetupReason ?? "AI setup is incomplete."
                    : "Local AI runtime setup completed.",
                Status: status));
        });

        app.MapGet("/api/runtime/models", async (
                IAiModelRegistry registry,
                CancellationToken cancellationToken) =>
            Results.Ok(await registry.ListModelsAsync(cancellationToken)));

        return app;
    }
}
