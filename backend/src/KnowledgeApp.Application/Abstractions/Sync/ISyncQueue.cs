using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Abstractions;

public interface ISyncQueue
{
    Task EnqueueAsync(Guid entityId, SyncOperation operation, CancellationToken cancellationToken = default);
}
