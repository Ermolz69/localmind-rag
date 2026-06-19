using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class QueuedIngestionJobDispatcherTests
{
    [Fact]
    public async Task RecoverPendingBatchAsync_Should_Process_Oldest_Queued_Jobs()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob newerJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        IngestionJob olderJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        IngestionJob runningJob = new()
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Processing,
        };
        database.Context.IngestionJobs.AddRange(newerJob, olderJob, runningJob);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            new IngestionJobRepository(database.Context),
            processor,
            Options.Create(new IngestionWorkerOptions { RecoveryBatchSize = 1 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        int processed = await dispatcher.RecoverPendingBatchAsync();

        Assert.Equal(1, processed);
        Assert.Single(processor.ProcessedJobIds);
        Assert.Equal(olderJob.Id, processor.ProcessedJobIds[0]);
        Assert.DoesNotContain(runningJob.Id, processor.ProcessedJobIds);
    }

    [Fact]
    public async Task RecoverPendingBatchAsync_Should_Respect_Cancellation()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        database.Context.IngestionJobs.Add(new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Pending });
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            new IngestionJobRepository(database.Context),
            processor,
            Options.Create(new IngestionWorkerOptions { RecoveryBatchSize = 1 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => dispatcher.RecoverPendingBatchAsync(cancellation.Token));
    }

    [Fact]
    public async Task RecoverPendingBatchAsync_Should_Process_Up_To_Configured_Batch_Size()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob firstJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        IngestionJob secondJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        IngestionJob thirdJob = new()
        {
            CreatedAt = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        database.Context.IngestionJobs.AddRange(firstJob, secondJob, thirdJob);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            new IngestionJobRepository(database.Context),
            processor,
            Options.Create(new IngestionWorkerOptions { RecoveryBatchSize = 2 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        int processed = await dispatcher.RecoverPendingBatchAsync();

        Assert.Equal(2, processed);
        Assert.Equal([firstJob.Id, secondJob.Id], processor.ProcessedJobIds);
        Assert.DoesNotContain(thirdJob.Id, processor.ProcessedJobIds);
    }

    [Fact]
    public async Task RecoverPendingBatchAsync_Should_Order_Equal_Timestamps_By_Id()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        DateTimeOffset createdAt = new(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);
        IngestionJob secondJob = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            CreatedAt = createdAt,
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        IngestionJob firstJob = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CreatedAt = createdAt,
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        database.Context.IngestionJobs.AddRange(secondJob, firstJob);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            new IngestionJobRepository(database.Context),
            processor,
            Options.Create(new IngestionWorkerOptions { RecoveryBatchSize = 2 }),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        int processed = await dispatcher.RecoverPendingBatchAsync();

        Assert.Equal(2, processed);
        Assert.Equal([firstJob.Id, secondJob.Id], processor.ProcessedJobIds);
    }

    [Fact]
    public async Task IngestionJobs_Should_Have_Recovery_Composite_Index()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using System.Data.Common.DbCommand command =
            database.Context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """PRAGMA index_info('IX_ingestion_jobs_Status_CreatedAtUnixTimeMs_Id');""";

        List<string> columns = [];
        await using System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(2));
        }

        Assert.Equal(["Status", "CreatedAtUnixTimeMs", "Id"], columns);
    }

    [Fact]
    public async Task ProcessJobAsync_Should_Process_Only_Requested_Job()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        Guid requestedJobId = Guid.NewGuid();
        FakeIngestionJobProcessor processor = new();
        QueuedIngestionJobDispatcher dispatcher = new(
            new IngestionJobRepository(database.Context),
            processor,
            Options.Create(new IngestionWorkerOptions()),
            NullLogger<QueuedIngestionJobDispatcher>.Instance);

        await dispatcher.ProcessJobAsync(requestedJobId);

        Assert.Equal([requestedJobId], processor.ProcessedJobIds);
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
