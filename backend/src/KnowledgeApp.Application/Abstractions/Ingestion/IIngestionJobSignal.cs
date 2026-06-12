namespace KnowledgeApp.Application.Abstractions.Ingestion;

public interface IIngestionJobSignal
{
    ValueTask<bool> PublishAsync(Guid jobId, CancellationToken cancellationToken = default);

    ValueTask<Guid> ReadAsync(CancellationToken cancellationToken = default);

    void Complete(Guid jobId);
}
