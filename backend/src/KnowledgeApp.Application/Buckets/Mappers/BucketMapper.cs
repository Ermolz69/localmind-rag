using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public static class BucketMapper
{
    public static BucketDto ToDto(Bucket bucket)
    {
        return new BucketDto(
            bucket.Id,
            bucket.Name,
            bucket.Description,
            (int)bucket.SyncStatus,
            bucket.CreatedAt,
            bucket.UpdatedAt);
    }
}
