using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Diagnostics;

public class OperationLogRepository : IOperationLogRepository
{
    private readonly AppDbContext _dbContext;

    public OperationLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(OperationLog log, CancellationToken cancellationToken)
    {
        _dbContext.OperationLogs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<OperationLog>> GetRecentLogsAsync(int limit, string? cursor, CancellationToken cancellationToken)
    {
        IQueryable<OperationLog> query = _dbContext.OperationLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(x => string.Compare(x.Id, cursor) < 0);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
