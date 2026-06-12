using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionQueueTests
{
    [Fact]
    public async Task EnqueueAsync_Should_Persist_And_Publish_Created_Job()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        FakeIngestionJobSignal signal = new()
        {
            OnPublishAsync = async (jobId, cancellationToken) =>
            {
                database.Context.ChangeTracker.Clear();
                Assert.True(await database.Context.IngestionJobs
                    .AnyAsync(job => job.Id == jobId, cancellationToken));
            },
        };
        IngestionQueue queue = new(
            new IngestionJobRepository(database.Context),
            signal,
            new FixedDateTimeProvider());
        Guid documentId = Guid.NewGuid();

        Guid jobId = await queue.EnqueueAsync(documentId);

        IngestionJob storedJob = await database.Context.IngestionJobs.FindAsync(jobId)
            ?? throw new InvalidOperationException("The ingestion job was not persisted.");
        Assert.Equal(documentId, storedJob.DocumentId);
        Assert.Equal(IngestionJobStatus.Pending, storedJob.Status);
        Assert.Equal([jobId], signal.PublishedJobIds);
    }

    [Fact]
    public async Task EnqueueAsync_Should_Leave_Durable_Job_When_Publish_Fails()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        FakeIngestionJobSignal signal = new()
        {
            PublishException = new InvalidOperationException("signal failed"),
        };
        IngestionQueue queue = new(
            new IngestionJobRepository(database.Context),
            signal,
            new FixedDateTimeProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => queue.EnqueueAsync(Guid.NewGuid()));

        IngestionJob storedJob = Assert.Single(database.Context.IngestionJobs);
        Assert.Equal(IngestionJobStatus.Pending, storedJob.Status);
    }
}
