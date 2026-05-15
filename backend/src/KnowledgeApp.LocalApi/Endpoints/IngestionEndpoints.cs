using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingestion/jobs/{id:guid}/process", async (
            Guid id,
            IIngestionJobProcessor processor,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var exists = await db.IngestionJobs.AnyAsync(x => x.Id == id, cancellationToken);
            if (!exists)
            {
                return Results.NotFound();
            }

            await processor.ProcessAsync(id, cancellationToken);
            return Results.Accepted();
        });

        return app;
    }
}
