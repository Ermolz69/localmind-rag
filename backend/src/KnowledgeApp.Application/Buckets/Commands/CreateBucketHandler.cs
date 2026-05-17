using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class CreateBucketHandler(
    IAppDbContext dbContext,
    BucketRequestValidator validator,
    ILocalDeviceResolver localDeviceResolver)
{
    public async Task<BucketDto> HandleAsync(CreateBucketRequest request, CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        Bucket bucket = new()
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            LocalDeviceId = localDeviceId,
        };

        dbContext.Buckets.Add(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return BucketMapper.ToDto(bucket);
    }
}
