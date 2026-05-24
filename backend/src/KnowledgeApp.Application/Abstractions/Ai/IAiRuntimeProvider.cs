using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiRuntimeProvider
{
    string ProviderId { get; }

    string ProviderName { get; }

    AiRuntimeProviderCapabilities Capabilities { get; }

    Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default);
}
