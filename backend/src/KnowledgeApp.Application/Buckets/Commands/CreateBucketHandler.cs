using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class CreateBucketHandler(IAppDbContext dbContext, BucketRequestValidator validator)
{
    public async Task<BucketDto> HandleAsync(CreateBucketRequest request, CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Bucket bucket = new()
        {
            Name = request.Name.Trim(),
            Description = request.Description,
        };

        dbContext.Buckets.Add(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return BucketMapper.ToDto(bucket);
    }
}
