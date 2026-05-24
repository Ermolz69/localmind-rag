using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Common;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class BucketEndpoints
{
    public static IEndpointRouteBuilder MapBucketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buckets", async (GetBucketsHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            ApiResults.Ok(await handler.HandleAsync(cancellationToken), context))
            .WithName("ListBuckets")
            .WithTags("Buckets")
            .WithSummary("Lists all buckets.")
            .WithDescription("Returns the buckets available on the local device.")
            .Produces<ApiResponse<IReadOnlyList<BucketDto>>>();

        app.MapGet("/api/buckets/page", async (
                string? query,
                string? cursor,
                int? limit,
                GetBucketsPageHandler handler,
                HttpContext context,
                CancellationToken cancellationToken) =>
            ApiResults.Ok(await handler.HandleAsync(new GetBucketsPageQuery(query, cursor, limit ?? 30), cancellationToken), context))
            .WithName("ListBucketsPage")
            .WithTags("Buckets")
            .WithSummary("Lists a cursor-paged bucket slice.")
            .WithDescription("Returns buckets filtered by optional search text and cursor pagination.")
            .Produces<ApiResponse<CursorPage<BucketDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/api/buckets", async (
            CreateBucketRequest request,
            CreateBucketHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            BucketDto created = await handler.HandleAsync(request, cancellationToken);
            return ApiResults.Created($"/api/buckets/{created.Id}", created, context);
        })
            .WithName("CreateBucket")
            .WithTags("Buckets")
            .WithSummary("Creates a bucket.")
            .WithDescription("Creates a local bucket used to group documents and notes.")
            .Produces<ApiResponse<BucketDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPut("/api/buckets/{id:guid}", async (
            Guid id,
            UpdateBucketRequest request,
            UpdateBucketHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            UpdateBucketResult result = await handler.HandleAsync(id, request, cancellationToken);
            if (!result.Found)
            {
                return ApiResults.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound), context);
            }

            return ApiResults.Empty(context);
        })
            .WithName("UpdateBucket")
            .WithTags("Buckets")
            .WithSummary("Updates a bucket.")
            .WithDescription("Updates the name and optional description of an existing local bucket.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/buckets/{id:guid}", async (
            Guid id,
            DeleteBucketHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            DeleteBucketResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return ApiResults.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound), context);
            }

            return ApiResults.Empty(context);
        })
            .WithName("DeleteBucket")
            .WithTags("Buckets")
            .WithSummary("Deletes a bucket.")
            .WithDescription("Deletes a local bucket when it exists.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }
}
