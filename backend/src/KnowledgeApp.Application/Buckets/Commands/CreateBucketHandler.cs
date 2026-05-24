using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Buckets;

public sealed class CreateBucketHandler(
    IAppDbContext dbContext,
    BucketRequestValidator validator,
    ILocalDeviceResolver localDeviceResolver)
{
    public async Task<Result<BucketDto>> HandleAsync(CreateBucketRequest request, CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return Result<BucketDto>.Failure(validation);
        }

        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        Bucket bucket = new()
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            LocalDeviceId = localDeviceId,
        };

        dbContext.Buckets.Add(bucket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<BucketDto>.Success(BucketMapper.ToDto(bucket));
    }
}
