using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class CreateBucketHandler(IAppDbContext dbContext)
{
    public async Task<BucketDto> HandleAsync(CreateBucketRequest request, CancellationToken cancellationToken = default)
    {
        var bucket = new Bucket
        {
            Name = request.Name,
            Description = request.Description,
        };

        dbContext.Buckets.Add(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return BucketMapper.ToDto(bucket);
    }
}
