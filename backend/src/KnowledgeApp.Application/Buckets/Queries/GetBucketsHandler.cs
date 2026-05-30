using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class GetBucketsHandler(IBucketRepository bucketRepository)
{
    public async Task<IReadOnlyList<BucketDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Entities.Bucket> buckets = await bucketRepository.ListAsync(cancellationToken);

        return buckets
            .OrderBy(bucket => bucket.Name)
            .Select(BucketMapper.ToDto)
            .ToArray();
    }
}
