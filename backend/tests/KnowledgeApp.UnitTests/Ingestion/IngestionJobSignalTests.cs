using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobSignalTests
{
    [Fact]
    public async Task Signal_Should_Read_Published_Jobs_In_Order()
    {
        IngestionJobSignal signal = new();
        Guid firstJobId = Guid.NewGuid();
        Guid secondJobId = Guid.NewGuid();

        Assert.True(await signal.PublishAsync(firstJobId));
        Assert.True(await signal.PublishAsync(secondJobId));

        Assert.Equal(firstJobId, await signal.ReadAsync());
        Assert.Equal(secondJobId, await signal.ReadAsync());
    }

    [Fact]
    public async Task Signal_Should_Deduplicate_Until_Job_Is_Completed()
    {
        IngestionJobSignal signal = new();
        Guid jobId = Guid.NewGuid();

        Assert.True(await signal.PublishAsync(jobId));
        Assert.False(await signal.PublishAsync(jobId));

        Assert.Equal(jobId, await signal.ReadAsync());
        signal.Complete(jobId);

        Assert.True(await signal.PublishAsync(jobId));
        Assert.Equal(jobId, await signal.ReadAsync());
    }

    [Fact]
    public async Task ReadAsync_Should_Respect_Cancellation()
    {
        IngestionJobSignal signal = new();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await signal.ReadAsync(cancellation.Token));
    }
}
