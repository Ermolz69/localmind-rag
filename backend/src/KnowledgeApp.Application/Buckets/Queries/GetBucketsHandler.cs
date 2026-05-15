using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Buckets;

public sealed class GetBucketsHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<Bucket>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Buckets
            .AsNoTracking()
            .OrderBy(bucket => bucket.Name)
            .ToArrayAsync(cancellationToken);
    }
}
