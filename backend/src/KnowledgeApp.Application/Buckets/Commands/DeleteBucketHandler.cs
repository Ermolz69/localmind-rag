using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class DeleteBucketHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Bucket? bucket = await dbContext.Buckets
            .FirstOrDefaultAsync(x => x.Id == bucketId && x.DeletedAt == null, cancellationToken);
        if (bucket is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound));
        }

        bucket.DeletedAt = dateTimeProvider.UtcNow;
        bucket.SyncStatus = SyncStatus.DeletedLocal;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
