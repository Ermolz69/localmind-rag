using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class UpdateBucketHandler(
    IBucketRepository bucketRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    BucketRequestValidator validator)
{
    public async Task<Result> HandleAsync(
        Guid bucketId,
        UpdateBucketRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Domain.Entities.Bucket? bucket = await bucketRepository.GetByIdAsync(bucketId, cancellationToken);
        if (bucket is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound));
        }

        bucket.Name = request.Name.Trim();
        bucket.Description = request.Description;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;

        await bucketRepository.UpdateAsync(bucket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
