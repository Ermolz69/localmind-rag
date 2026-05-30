using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class BucketRepository(AppDbContext dbContext) : IBucketRepository
{
    public async Task<Bucket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Buckets
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, cancellationToken);
    }

    public async Task<Bucket?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Buckets
            .FirstOrDefaultAsync(item => item.Name == name && item.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Bucket>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Buckets
            .Where(bucket => bucket.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bucket>> ListPagedAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        List<Bucket> all = await dbContext.Buckets
            .Where(bucket => bucket.DeletedAt == null)
            .ToListAsync(cancellationToken);

        IEnumerable<Bucket> result = all.OrderByDescending(bucket => bucket.CreatedAt);

        if (offset > 0)
        {
            result = result.Skip(offset);
        }

        if (limit > 0)
        {
            result = result.Take(limit);
        }

        return result.ToList();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Buckets
            .CountAsync(bucket => bucket.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Bucket bucket, CancellationToken cancellationToken = default)
    {
        await dbContext.Buckets.AddAsync(bucket, cancellationToken);
    }

    public Task UpdateAsync(Bucket bucket, CancellationToken cancellationToken = default)
    {
        dbContext.Buckets.Update(bucket);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Bucket bucket, CancellationToken cancellationToken = default)
    {
        dbContext.Buckets.Remove(bucket);
        return Task.CompletedTask;
    }
}
