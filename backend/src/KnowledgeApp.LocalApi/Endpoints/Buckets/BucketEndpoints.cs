using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Buckets;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class BucketEndpoints
{
    public static IEndpointRouteBuilder MapBucketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buckets", async (GetBucketsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        app.MapPost("/api/buckets", async (
            CreateBucketRequest request,
            CreateBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            BucketDto created = await handler.HandleAsync(request, cancellationToken);
            return Results.Created($"/api/buckets/{created.Id}", created);
        });

        app.MapPut("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            UpdateBucketRequest request,
            UpdateBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            UpdateBucketResult result = await handler.HandleAsync(id, request, cancellationToken);
            if (!result.Found)
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
            DeleteBucketResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        return app;
    }
}
