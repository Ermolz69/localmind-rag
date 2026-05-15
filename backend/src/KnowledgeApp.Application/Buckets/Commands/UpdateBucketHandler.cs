using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class UpdateBucketHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    BucketRequestValidator validator)
{
    public async Task<UpdateBucketResult> HandleAsync(
        Guid bucketId,
        UpdateBucketRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Domain.Entities.Bucket? bucket = await dbContext.Buckets
            .FirstOrDefaultAsync(x => x.Id == bucketId, cancellationToken);
        if (bucket is null)
        {
            return new UpdateBucketResult(false);
        }

        bucket.Name = request.Name.Trim();
        bucket.Description = request.Description;
        bucket.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new UpdateBucketResult(true);
    }
}
