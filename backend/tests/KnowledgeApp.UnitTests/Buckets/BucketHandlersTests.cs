using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Buckets;

public sealed class BucketHandlersTests
{
    [Fact]
    public async Task BucketHandlers_Should_List_Create_Update_And_Delete()
    {
        await using var database = await ApplicationTestDatabase.CreateAsync();
        var create = new CreateBucketHandler(database.Context);
        var list = new GetBucketsHandler(database.Context);
        var update = new UpdateBucketHandler(database.Context, new FixedDateTimeProvider());
        var delete = new DeleteBucketHandler(database.Context);

        var bucket = await create.HandleAsync(new CreateBucketRequest("Work", "Initial"));
        var buckets = await list.HandleAsync();
        var updateResult = await update.HandleAsync(bucket.Id, new UpdateBucketRequest("Personal", "Updated"));
        var missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateBucketRequest("Missing", null));
        var deleteResult = await delete.HandleAsync(bucket.Id);
        var missingDeleteResult = await delete.HandleAsync(bucket.Id);

        Assert.Contains(buckets, item => item.Id == bucket.Id);
        Assert.True(updateResult.Found);
        Assert.False(missingUpdateResult.Found);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
    }
}
