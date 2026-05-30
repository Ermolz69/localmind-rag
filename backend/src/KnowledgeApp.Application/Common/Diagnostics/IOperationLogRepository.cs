using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Common.Diagnostics;

public interface IOperationLogRepository
{
    Task AddAsync(OperationLog log, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationLog>> GetRecentLogsAsync(int limit, string? cursor, CancellationToken cancellationToken);
}
