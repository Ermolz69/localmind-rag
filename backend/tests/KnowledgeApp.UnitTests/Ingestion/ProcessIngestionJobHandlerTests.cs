using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class ProcessIngestionJobHandlerTests
{
    [Fact]
    public async Task ProcessIngestionJobHandler_Should_Return_NotFound_Or_Call_Processor()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        IngestionJob? job = new IngestionJob { DocumentId = Guid.NewGuid() };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        FakeIngestionJobProcessor? processor = new FakeIngestionJobProcessor();
        ProcessIngestionJobHandler? handler = new ProcessIngestionJobHandler(database.Context, processor);

        ProcessIngestionJobResult? missingResult = await handler.HandleAsync(Guid.NewGuid());
        ProcessIngestionJobResult? result = await handler.HandleAsync(job.Id);

        Assert.False(missingResult.Found);
        Assert.True(result.Found);
        Assert.Equal(job.Id, processor.LastProcessedJobId);
    }

    private sealed class FakeIngestionJobProcessor : IIngestionJobProcessor
    {
        public Guid? LastProcessedJobId { get; private set; }

        public Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            LastProcessedJobId = jobId;
            return Task.CompletedTask;
        }
    }
}
