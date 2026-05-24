using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobLifecycleHandlerTests
{
    [Fact]
    public async Task Retry_Should_Requeue_Failed_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob job = new() { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Failed, LastError = "failed", AttemptCount = 1 };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        RetryIngestionJobHandler handler = new(database.Context, new FixedDateTimeProvider());

        IngestionJobActionResponse response = (await handler.HandleAsync(job.Id)).AssertSuccess();

        IngestionJob stored = await database.Context.IngestionJobs.FindAsync(job.Id) ?? throw new InvalidOperationException();
        Assert.Equal(IngestionJobStatus.Queued, stored.Status);
        Assert.Null(stored.LastError);
        Assert.Equal(job.Id, response.JobId);
    }

    [Fact]
    public async Task Cancel_Should_Reject_Completed_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob job = new() { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Completed };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        CancelIngestionJobHandler handler = new(database.Context, new FixedDateTimeProvider());

        Result<IngestionJobActionResponse> result = await handler.HandleAsync(job.Id);

        Assert.Equal("INGESTION_JOB_NOT_CANCELLABLE", result.AssertFailure(ErrorType.Conflict).Code);
    }

    [Fact]
    public async Task List_Should_Return_Job_Affordances()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        database.Context.IngestionJobs.AddRange(
            new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Failed, AttemptCount = 2 },
            new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Queued });
        await database.Context.SaveChangesAsync();
        ListIngestionJobsHandler handler = new(database.Context);

        IngestionJobListResponse response = (await handler.HandleAsync(new ListIngestionJobsQuery())).AssertSuccess();

        Assert.Equal(2, response.TotalCount);
        Assert.Contains(response.Items, item => item.Status == "Failed" && item.CanRetry);
        Assert.Contains(response.Items, item => item.Status == "Queued" && item.CanCancel);
    }
}
