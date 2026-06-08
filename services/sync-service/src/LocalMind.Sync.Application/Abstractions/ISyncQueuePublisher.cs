namespace LocalMind.Sync.Application.Abstractions;

public interface ISyncQueuePublisher
{
    Task<Guid> PublishPushRequestedAsync(Guid deviceId, int changeCount, CancellationToken cancellationToken);

    Task<Guid> PublishPullRequestedAsync(Guid deviceId, int limit, CancellationToken cancellationToken);

    Task<Guid> PublishDiffRequestedAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<Guid> PublishConflictDetectedAsync(Guid conflictId, string strategy, CancellationToken cancellationToken);
}
