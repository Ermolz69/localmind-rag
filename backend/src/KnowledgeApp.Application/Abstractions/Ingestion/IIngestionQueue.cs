namespace KnowledgeApp.Application.Abstractions;

public interface IIngestionQueue
{
    Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);
}
