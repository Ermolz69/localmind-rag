using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IBucketRepository
{
    Task<Bucket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Bucket?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bucket>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bucket>> ListPagedAsync(int limit, int offset, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Bucket bucket, CancellationToken cancellationToken = default);
    Task UpdateAsync(Bucket bucket, CancellationToken cancellationToken = default);
    Task DeleteAsync(Bucket bucket, CancellationToken cancellationToken = default);
}
