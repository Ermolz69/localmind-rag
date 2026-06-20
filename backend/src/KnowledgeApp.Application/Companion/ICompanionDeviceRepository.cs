using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Companion;

/// <summary>Persistence for trusted Companion Mode devices.</summary>
public interface ICompanionDeviceRepository
{
    Task AddAsync(CompanionDevice device, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanionDevice>> ListAsync(CancellationToken cancellationToken = default);

    Task<CompanionDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<CompanionDevice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
