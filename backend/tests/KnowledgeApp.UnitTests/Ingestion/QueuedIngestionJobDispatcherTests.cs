using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class QueuedIngestionJobDispatcherTests
{
    [Fact]
    public async Task ProcessNextBatchAsync_Should_Process_Oldest_Queued_Jobs()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob newerJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
        };
        IngestionJob olderJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
        };
        IngestionJob runningJob = new()
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Running,
        };
        database.Context.IngestionJobs.AddRange(newerJob, olderJob, runningJob);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            database.Context,
            processor,
            Options.Create(new IngestionWorkerOptions { BatchSize = 1 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        int processed = await dispatcher.ProcessNextBatchAsync();

        Assert.Equal(1, processed);
        Assert.Single(processor.ProcessedJobIds);
        Assert.Equal(olderJob.Id, processor.ProcessedJobIds[0]);
        Assert.DoesNotContain(runningJob.Id, processor.ProcessedJobIds);
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Respect_Cancellation()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        database.Context.IngestionJobs.Add(new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Queued });
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            database.Context,
            processor,
            Options.Create(new IngestionWorkerOptions { BatchSize = 1 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.ProcessNextBatchAsync(cancellation.Token));
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Process_Up_To_Configured_Batch_Size()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob firstJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
        };
        IngestionJob secondJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
        };
        IngestionJob thirdJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
        };
        database.Context.IngestionJobs.AddRange(firstJob, secondJob, thirdJob);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            database.Context,
            processor,
            Options.Create(new IngestionWorkerOptions { BatchSize = 2 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        int processed = await dispatcher.ProcessNextBatchAsync();

        Assert.Equal(2, processed);
        Assert.Equal([firstJob.Id, secondJob.Id], processor.ProcessedJobIds);
        Assert.DoesNotContain(thirdJob.Id, processor.ProcessedJobIds);
    }

    private sealed class FakeIngestionJobProcessor : IIngestionJobProcessor
    {
        public List<Guid> ProcessedJobIds { get; } = [];

        public Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessedJobIds.Add(jobId);
            return Task.CompletedTask;
        }
    }
}
