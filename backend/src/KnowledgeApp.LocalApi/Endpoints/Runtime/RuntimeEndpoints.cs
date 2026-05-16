using KnowledgeApp.Application.Abstractions;

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

        app.MapGet("/api/runtime/models", async (
                IAiModelRegistry registry,
                CancellationToken cancellationToken) =>
            Results.Ok(await registry.ListModelsAsync(cancellationToken)));

        return app;
    }
}
