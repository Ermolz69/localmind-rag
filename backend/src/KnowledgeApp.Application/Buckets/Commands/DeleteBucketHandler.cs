using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class DeleteBucketHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<DeleteBucketResult> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Bucket? bucket = await dbContext.Buckets
            .FirstOrDefaultAsync(x => x.Id == bucketId && x.DeletedAt == null, cancellationToken);
        if (bucket is null)
        {
            return new DeleteBucketResult(false);
        }

        bucket.DeletedAt = dateTimeProvider.UtcNow;
        bucket.SyncStatus = SyncStatus.DeletedLocal;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteBucketResult(true);
    }
}
