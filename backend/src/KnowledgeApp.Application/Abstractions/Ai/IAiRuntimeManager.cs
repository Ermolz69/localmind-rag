using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiRuntimeManager
{
    Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);
}

public interface IAiRuntimeSetupService
{
    Task SetupAsync(IProgress<RuntimeSetupProgress>? progress = null, CancellationToken cancellationToken = default);
}

public interface IAiRuntimeSetupCoordinator
{
    RuntimeSetupStartedResponse StartSetup(CancellationToken cancellationToken = default);

    IAsyncEnumerable<RuntimeSetupProgress> WatchProgressAsync(Guid setupId, CancellationToken cancellationToken = default);
}
