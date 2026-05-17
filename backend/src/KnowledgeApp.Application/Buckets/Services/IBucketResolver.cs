using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public interface IBucketResolver
{
    Task<Bucket> ResolveForUploadAsync(Guid? requestedBucketId, CancellationToken cancellationToken = default);
}
