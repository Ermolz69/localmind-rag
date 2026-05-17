using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface ISyncService
{
    Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task RunAsync(CancellationToken cancellationToken = default);
}
