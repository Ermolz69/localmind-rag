using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class CreateBucketHandler(IAppDbContext dbContext)
{
    public async Task<Bucket> HandleAsync(Bucket bucket, CancellationToken cancellationToken = default)
    {
        dbContext.Buckets.Add(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return bucket;
    }
}
