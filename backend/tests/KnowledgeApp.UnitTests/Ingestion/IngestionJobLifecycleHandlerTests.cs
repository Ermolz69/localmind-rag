using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobLifecycleHandlerTests
{
    [Fact]
    public async Task Retry_Should_Requeue_Failed_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob job = new()
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Failed,
            ErrorCode = "INGESTION_JOB_FAILED",
            ErrorMessage = "failed",
            RetryCount = 1,
        };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        var documentRepository = new KnowledgeApp.Infrastructure.Services.Persistence.DocumentRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        RetryIngestionJobHandler handler = new(documentRepository, new IngestionJobRepository(database.Context), unitOfWork, new FixedDateTimeProvider());

        IngestionJobActionResponse response = (await handler.HandleAsync(job.Id)).AssertSuccess();

        IngestionJob stored = await database.Context.IngestionJobs.FindAsync(job.Id) ?? throw new InvalidOperationException();
        Assert.Equal(IngestionJobStatus.Pending, stored.Status);
        Assert.Null(stored.ErrorCode);
        Assert.Null(stored.ErrorMessage);
        Assert.Equal(2, stored.RetryCount);
        Assert.Equal(job.Id, response.JobId);
    }

    [Fact]
    public async Task Cancel_Should_Reject_Indexed_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob job = new() { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Indexed };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        var documentRepository = new KnowledgeApp.Infrastructure.Services.Persistence.DocumentRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CancelIngestionJobHandler handler = new(documentRepository, new IngestionJobRepository(database.Context), unitOfWork, new FixedDateTimeProvider());

        Result<IngestionJobActionResponse> result = await handler.HandleAsync(job.Id);

        Assert.Equal("INGESTION_JOB_NOT_CANCELLABLE", result.AssertFailure(ErrorType.Conflict).Code);
    }

    [Fact]
    public async Task List_Should_Return_Job_Affordances()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        database.Context.IngestionJobs.AddRange(
            new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Failed, RetryCount = 2 },
            new IngestionJob { DocumentId = Guid.NewGuid(), Status = IngestionJobStatus.Pending });
        await database.Context.SaveChangesAsync();
        ListIngestionJobsHandler handler = new(new IngestionJobRepository(database.Context));

        IngestionJobListResponse response = (await handler.HandleAsync(new ListIngestionJobsQuery())).AssertSuccess();

        Assert.Equal(2, response.TotalCount);
        Assert.Contains(response.Items, item => item.Status == "Failed" && item.CanRetry);
        Assert.Contains(response.Items, item => item.Status == "Pending" && item.CanCancel);
    }
}
