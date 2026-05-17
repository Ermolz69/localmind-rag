using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface ILocalDiagnosticsService
{
    Task<DiagnosticsDto> GetAsync(CancellationToken cancellationToken = default);
}
