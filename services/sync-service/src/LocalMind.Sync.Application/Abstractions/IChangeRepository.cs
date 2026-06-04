namespace LocalMind.Sync.Application.Abstractions;

using LocalMind.Sync.Domain.Changes;

public interface IChangeRepository
{
    Task SaveManyAsync(IReadOnlyList<SyncChange> changes, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncChange>> PullAsync(Guid deviceId, string cursor, int limit, CancellationToken cancellationToken);
}
