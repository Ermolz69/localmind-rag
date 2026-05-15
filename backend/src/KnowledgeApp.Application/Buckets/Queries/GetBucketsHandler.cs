using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class GetBucketsHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<BucketDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        Domain.Entities.Bucket[]? buckets = await dbContext.Buckets
            .AsNoTracking()
            .OrderBy(bucket => bucket.Name)
            .ToArrayAsync(cancellationToken);

        return buckets.Select(BucketMapper.ToDto).ToArray();
    }
}
