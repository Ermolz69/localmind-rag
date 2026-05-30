using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class BucketResolver(
    IBucketRepository bucketRepository,
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IBucketResolver
{
    public async Task<Result<Bucket>> ResolveForUploadAsync(Guid? requestedBucketId, CancellationToken cancellationToken = default)
    {
        if (requestedBucketId.HasValue)
        {
            Bucket? requestedBucket = await bucketRepository.GetByIdAsync(requestedBucketId.Value, cancellationToken);
            if (requestedBucket is null)
            {
                return Result<Bucket>.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound));
            }

            await SetLastSelectedBucketAsync(requestedBucket.Id, cancellationToken);
            return Result<Bucket>.Success(requestedBucket);
        }

        Bucket? lastSelectedBucket = await GetLastSelectedBucketAsync(cancellationToken);
        if (lastSelectedBucket is not null)
        {
            return Result<Bucket>.Success(lastSelectedBucket);
        }

        Bucket? defaultBucket = await bucketRepository.GetByNameAsync(BucketConstants.DefaultBucketName, cancellationToken);

        if (defaultBucket is null)
        {
            defaultBucket = new Bucket
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Name = BucketConstants.DefaultBucketName,
                SyncStatus = SyncStatus.LocalOnly,
            };
            await bucketRepository.AddAsync(defaultBucket, cancellationToken);
        }

        await SetLastSelectedBucketAsync(defaultBucket.Id, cancellationToken);
        return Result<Bucket>.Success(defaultBucket);
    }

    private async Task<Bucket?> GetLastSelectedBucketAsync(CancellationToken cancellationToken)
    {
        AppSetting? setting = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == BucketSettingsKeys.LastSelectedBucketId, cancellationToken);

        if (setting is null || !Guid.TryParse(setting.Value, out Guid bucketId))
        {
            return null;
        }

        return await bucketRepository.GetByIdAsync(bucketId, cancellationToken);
    }

    private async Task SetLastSelectedBucketAsync(Guid bucketId, CancellationToken cancellationToken)
    {
        AppSetting? setting = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == BucketSettingsKeys.LastSelectedBucketId, cancellationToken);

        if (setting is null)
        {
            dbContext.AppSettings.Add(new AppSetting
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Key = BucketSettingsKeys.LastSelectedBucketId,
                Value = bucketId.ToString(),
            });
            return;
        }

        setting.Value = bucketId.ToString();
        setting.UpdatedAt = dateTimeProvider.UtcNow;
    }
}
