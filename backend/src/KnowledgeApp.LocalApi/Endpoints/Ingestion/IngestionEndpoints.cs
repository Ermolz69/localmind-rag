using KnowledgeApp.Application.Ingestion;

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
            var result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return Results.NotFound();
            }

            return Results.Accepted();
        });

        return app;
    }
}
