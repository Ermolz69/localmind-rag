using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class DeleteBucketHandler(
    IBucketRepository bucketRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Bucket? bucket = await bucketRepository.GetByIdAsync(bucketId, cancellationToken);
        if (bucket is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound));
        }

        bucket.DeletedAt = dateTimeProvider.UtcNow;
        bucket.SyncStatus = SyncStatus.DeletedLocal;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;
        await bucketRepository.UpdateAsync(bucket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
