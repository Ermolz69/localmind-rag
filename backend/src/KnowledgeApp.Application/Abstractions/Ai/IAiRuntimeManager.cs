using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiRuntimeManager
{
    Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);
}

public interface IAiRuntimeSetupService
{
    Task SetupAsync(CancellationToken cancellationToken = default);
}
