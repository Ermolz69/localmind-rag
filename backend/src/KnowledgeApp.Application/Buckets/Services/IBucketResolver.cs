using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public interface IBucketResolver
{
    Task<Result<Bucket>> ResolveForUploadAsync(Guid? requestedBucketId, CancellationToken cancellationToken = default);
}
