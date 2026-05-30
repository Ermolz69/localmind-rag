using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface ILocalDiagnosticsService
{
    Task<DiagnosticsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsDatabaseDto> GetDatabaseAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsStorageDto> GetStorageAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsVectorIndexDto> GetVectorIndexAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsRuntimeDto> GetRuntimeAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsHealthStatus> GetGeneralHealthAsync(CancellationToken cancellationToken = default);
}
