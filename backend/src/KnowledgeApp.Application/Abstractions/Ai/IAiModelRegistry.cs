using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiModelRegistry
{
    Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default);
}
