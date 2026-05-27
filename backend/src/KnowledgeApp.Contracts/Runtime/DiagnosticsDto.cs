namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Aggregated local diagnostics payload.</summary>
/// <param name="Database">Database diagnostics.</param>
/// <param name="Storage">Storage usage by local runtime area.</param>
/// <param name="VectorIndex">Vector index diagnostics.</param>
/// <param name="Runtime">Runtime mode and AI runtime diagnostics.</param>
/// <param name="LatestErrors">Latest ingestion errors.</param>
public sealed record DiagnosticsDto(
    DiagnosticsDatabaseDto Database,
    DiagnosticsStorageDto Storage,
    DiagnosticsVectorIndexDto VectorIndex,
    DiagnosticsRuntimeDto Runtime,
    IReadOnlyList<DiagnosticsIngestionErrorDto> LatestErrors)
{
    public DiagnosticsHealthStatus Status => 
        Database.Status == DiagnosticsHealthStatus.Healthy &&
        Storage.Status == DiagnosticsHealthStatus.Healthy &&
        VectorIndex.Status == DiagnosticsHealthStatus.Healthy &&
        Runtime.Status == DiagnosticsHealthStatus.Healthy
            ? DiagnosticsHealthStatus.Healthy
            : (Database.Status == DiagnosticsHealthStatus.Unhealthy ||
               Storage.Status == DiagnosticsHealthStatus.Unhealthy ||
               VectorIndex.Status == DiagnosticsHealthStatus.Unhealthy ||
               Runtime.Status == DiagnosticsHealthStatus.Unhealthy
                ? DiagnosticsHealthStatus.Unhealthy
                : DiagnosticsHealthStatus.Degraded);
}

