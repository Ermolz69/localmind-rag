namespace KnowledgeApp.Application.Abstractions;

public interface IIngestionQueue
{
    Task<Guid> EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);
}
