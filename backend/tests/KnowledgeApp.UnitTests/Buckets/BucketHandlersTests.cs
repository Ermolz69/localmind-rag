using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Buckets;

public sealed class BucketHandlersTests
{
    [Fact]
    public async Task BucketHandlers_Should_List_Create_Update_And_Delete()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        BucketRequestValidator validator = new();
        CreateBucketHandler create = new(database.Context, validator);
        GetBucketsHandler list = new(database.Context);
        UpdateBucketHandler update = new(database.Context, new FixedDateTimeProvider(), validator);
        DeleteBucketHandler delete = new(database.Context);

        BucketDto? bucket = await create.HandleAsync(new CreateBucketRequest("Work", "Initial"));
        IReadOnlyList<BucketDto>? buckets = await list.HandleAsync();
        UpdateBucketResult? updateResult = await update.HandleAsync(bucket.Id, new UpdateBucketRequest("Personal", "Updated"));
        UpdateBucketResult? missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateBucketRequest("Missing", null));
        DeleteBucketResult? deleteResult = await delete.HandleAsync(bucket.Id);
        DeleteBucketResult? missingDeleteResult = await delete.HandleAsync(bucket.Id);

        Assert.Contains(buckets, item => item.Id == bucket.Id);
        Assert.True(updateResult.Found);
        Assert.False(missingUpdateResult.Found);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
    }
}
