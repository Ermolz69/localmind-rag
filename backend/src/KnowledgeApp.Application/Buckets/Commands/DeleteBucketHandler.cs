using KnowledgeApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class DeleteBucketHandler(IAppDbContext dbContext)
{
    public async Task<DeleteBucketResult> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Bucket? bucket = await dbContext.Buckets.FirstOrDefaultAsync(x => x.Id == bucketId, cancellationToken);
        if (bucket is null)
        {
            return new DeleteBucketResult(false);
        }

        dbContext.Buckets.Remove(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteBucketResult(true);
    }
}
