using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class BucketResolver(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider) : IBucketResolver
{
    public async Task<Bucket> ResolveForUploadAsync(Guid? requestedBucketId, CancellationToken cancellationToken = default)
    {
        if (requestedBucketId.HasValue)
        {
            var requestedBucket = await dbContext.Buckets.FindAsync([requestedBucketId.Value], cancellationToken);
            if (requestedBucket is null)
            {
                throw new NotFoundAppException("buckets.notFound", "Selected bucket was not found.");
            }

            await SetLastSelectedBucketAsync(requestedBucket.Id, cancellationToken);
            return requestedBucket;
        }

        var lastSelectedBucket = await GetLastSelectedBucketAsync(cancellationToken);
        if (lastSelectedBucket is not null)
        {
            return lastSelectedBucket;
        }

        var defaultBucket = await dbContext.Buckets
            .FirstOrDefaultAsync(x => x.Name == BucketConstants.DefaultBucketName, cancellationToken);

        if (defaultBucket is null)
        {
            defaultBucket = new Bucket
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Name = BucketConstants.DefaultBucketName,
                SyncStatus = SyncStatus.LocalOnly,
            };
            dbContext.Buckets.Add(defaultBucket);
        }

        await SetLastSelectedBucketAsync(defaultBucket.Id, cancellationToken);
        return defaultBucket;
    }

    private async Task<Bucket?> GetLastSelectedBucketAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == BucketSettingsKeys.LastSelectedBucketId, cancellationToken);

        if (setting is null || !Guid.TryParse(setting.Value, out var bucketId))
        {
            return null;
        }

        return await dbContext.Buckets.FindAsync([bucketId], cancellationToken);
    }

    private async Task SetLastSelectedBucketAsync(Guid bucketId, CancellationToken cancellationToken)
    {
        var setting = await dbContext.AppSettings
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
