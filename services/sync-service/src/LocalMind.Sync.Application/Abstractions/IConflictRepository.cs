namespace LocalMind.Sync.Application.Abstractions;

using LocalMind.Sync.Domain.Conflicts;

public interface IConflictRepository
{
    Task<IReadOnlyList<SyncConflict>> ListOpenAsync(CancellationToken cancellationToken);

    Task<SyncConflict?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SyncConflict> SaveAsync(SyncConflict conflict, CancellationToken cancellationToken);
}
