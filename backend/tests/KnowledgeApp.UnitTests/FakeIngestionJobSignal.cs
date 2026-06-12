using KnowledgeApp.Application.Abstractions.Ingestion;

namespace KnowledgeApp.UnitTests;

internal sealed class FakeIngestionJobSignal : IIngestionJobSignal
{
    public List<Guid> PublishedJobIds { get; } = [];

    public Exception? PublishException { get; set; }

    public Func<Guid, CancellationToken, ValueTask>? OnPublishAsync { get; set; }

    public ValueTask<bool> PublishAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (PublishException is not null)
        {
            return ValueTask.FromException<bool>(PublishException);
        }

        return PublishCoreAsync(jobId, cancellationToken);
    }

    private async ValueTask<bool> PublishCoreAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (OnPublishAsync is not null)
        {
            await OnPublishAsync(jobId, cancellationToken);
        }

        PublishedJobIds.Add(jobId);
        return true;
    }

    public ValueTask<Guid> ReadAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromException<Guid>(
            new NotSupportedException("The fake signal does not provide a reader."));
    }

    public void Complete(Guid jobId)
    {
    }
}
