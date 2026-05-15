using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class BucketEndpoints
{
    public static IEndpointRouteBuilder MapBucketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buckets", async (GetBucketsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        app.MapPost("/api/buckets", async (
            Bucket bucket,
            CreateBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            var created = await handler.HandleAsync(bucket, cancellationToken);
            return Results.Created($"/api/buckets/{created.Id}", created);
        });

        app.MapPut("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            Bucket request,
            UpdateBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!await handler.HandleAsync(id, request, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        app.MapDelete("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            DeleteBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!await handler.HandleAsync(id, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        return app;
    }
}
