using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Application.Buckets;

public sealed class DeleteBucketHandler(IAppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        var bucket = await dbContext.Buckets.FindAsync([bucketId], cancellationToken);
        if (bucket is null)
        {
            return false;
        }

        dbContext.Buckets.Remove(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
