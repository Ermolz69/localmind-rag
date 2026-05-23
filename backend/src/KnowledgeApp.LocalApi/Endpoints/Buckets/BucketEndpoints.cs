using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class BucketEndpoints
{
    public static IEndpointRouteBuilder MapBucketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buckets", async (GetBucketsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListBuckets")
            .WithTags("Buckets")
            .WithSummary("Lists all buckets.")
            .WithDescription("Returns the buckets available on the local device.")
            .Produces<IReadOnlyList<BucketDto>>();

        app.MapGet("/api/buckets/page", async (
                string? query,
                string? cursor,
                int? limit,
                GetBucketsPageHandler handler,
                CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetBucketsPageQuery(query, cursor, limit ?? 30), cancellationToken)))
            .WithName("ListBucketsPage")
            .WithTags("Buckets")
            .WithSummary("Lists a cursor-paged bucket slice.")
            .WithDescription("Returns buckets filtered by optional search text and cursor pagination.")
            .Produces<CursorPage<BucketDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/api/buckets", async (
            CreateBucketRequest request,
            CreateBucketHandler handler,
            CancellationToken cancellationToken) =>
        {
            BucketDto created = await handler.HandleAsync(request, cancellationToken);
            return Results.Created($"/api/buckets/{created.Id}", created);
        })
            .WithName("CreateBucket")
            .WithTags("Buckets")
            .WithSummary("Creates a bucket.")
            .WithDescription("Creates a local bucket used to group documents and notes.")
            .Produces<BucketDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
        })
            .WithName("UpdateBucket")
            .WithTags("Buckets")
            .WithSummary("Updates a bucket.")
            .WithDescription("Updates the name and optional description of an existing local bucket.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
        })
            .WithName("DeleteBucket")
            .WithTags("Buckets")
            .WithSummary("Deletes a bucket.")
            .WithDescription("Deletes a local bucket when it exists.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
