using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class BucketEndpoints
{
    public static IEndpointRouteBuilder MapBucketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buckets", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.Buckets.OrderBy(x => x.Name).ToArrayAsync(cancellationToken)));

        app.MapPost("/api/buckets", async (Bucket bucket, AppDbContext db, CancellationToken cancellationToken) =>
        {
            db.Buckets.Add(bucket);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/buckets/{bucket.Id}", bucket);
        });

        app.MapPut("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            Bucket request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var bucket = await db.Buckets.FindAsync([id], cancellationToken);
            if (bucket is null)
            {
                return TypedResults.NotFound();
            }

            bucket.Name = request.Name;
            bucket.Description = request.Description;
            bucket.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        });

        app.MapDelete("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var bucket = await db.Buckets.FindAsync([id], cancellationToken);
            if (bucket is null)
            {
                return TypedResults.NotFound();
            }

            db.Buckets.Remove(bucket);
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        });

        return app;
    }
}
