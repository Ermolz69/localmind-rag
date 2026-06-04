namespace LocalMind.Sync.Application.Abstractions;

using LocalMind.Sync.Domain.Sessions;

public interface ISyncSessionRepository
{
    Task<SyncSession> SaveAsync(SyncSession session, CancellationToken cancellationToken);

    Task<SyncSession?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
