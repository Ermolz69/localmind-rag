using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class UpdateBucketHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<bool> HandleAsync(Guid bucketId, Bucket request, CancellationToken cancellationToken = default)
    {
        var bucket = await dbContext.Buckets.FindAsync([bucketId], cancellationToken);
        if (bucket is null)
        {
            return false;
        }

        bucket.Name = request.Name;
        bucket.Description = request.Description;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
