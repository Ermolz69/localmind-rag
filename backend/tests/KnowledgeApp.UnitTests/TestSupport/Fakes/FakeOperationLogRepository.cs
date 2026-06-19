using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.UnitTests.TestSupport.Fakes;

internal sealed class FakeOperationLogRepository : IOperationLogRepository
{
    public Task AddAsync(OperationLog log, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<OperationLog>> GetRecentLogsAsync(int limit, string? cursor, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OperationLog>>([]);
}
