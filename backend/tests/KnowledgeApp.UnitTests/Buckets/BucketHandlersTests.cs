using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Buckets;

public sealed class BucketHandlersTests
{
    [Fact]
    public async Task BucketHandlers_Should_List_Create_Update_And_Delete()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        BucketRequestValidator validator = new();
        CreateBucketHandler create = new(database.Context, validator, new FakeLocalDeviceResolver());
        GetBucketsHandler list = new(database.Context);
        UpdateBucketHandler update = new(database.Context, new FixedDateTimeProvider(), validator);
        DeleteBucketHandler delete = new(database.Context, new FixedDateTimeProvider());

        BucketDto? bucket = (await create.HandleAsync(new CreateBucketRequest("Work", "Initial"))).AssertSuccess();
        IReadOnlyList<BucketDto>? buckets = await list.HandleAsync();
        Result updateResult = await update.HandleAsync(bucket.Id, new UpdateBucketRequest("Personal", "Updated"));
        Result missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateBucketRequest("Missing", null));
        Result deleteResult = await delete.HandleAsync(bucket.Id);
        Result missingDeleteResult = await delete.HandleAsync(bucket.Id);
        Domain.Entities.Bucket storedBucket = await database.Context.Buckets.SingleAsync(item => item.Id == bucket.Id);
        IReadOnlyList<BucketDto> visibleBuckets = await list.HandleAsync();

        Assert.Contains(buckets, item => item.Id == bucket.Id);
        updateResult.AssertSuccess();
        Assert.Equal("BUCKET_NOT_FOUND", missingUpdateResult.AssertFailure().Code);
        deleteResult.AssertSuccess();
        Assert.Equal("BUCKET_NOT_FOUND", missingDeleteResult.AssertFailure().Code);
        Assert.NotNull(storedBucket.DeletedAt);
        Assert.Equal(SyncStatus.DeletedLocal, storedBucket.SyncStatus);
        Assert.DoesNotContain(visibleBuckets, item => item.Id == bucket.Id);
    }
}
