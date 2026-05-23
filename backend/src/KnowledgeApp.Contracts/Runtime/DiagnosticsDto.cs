namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Aggregated local diagnostics payload.</summary>
/// <param name="Paths">Resolved runtime paths.</param>
/// <param name="Storage">Storage usage by local runtime area.</param>
/// <param name="Counts">Entity and queue counts.</param>
/// <param name="LatestErrors">Latest ingestion errors.</param>
/// <param name="Runtime">Runtime mode and AI runtime diagnostics.</param>
public sealed record DiagnosticsDto(
    DiagnosticsPathsDto Paths,
    DiagnosticsStorageDto Storage,
    DiagnosticsCountsDto Counts,
    IReadOnlyList<DiagnosticsIngestionErrorDto> LatestErrors,
    DiagnosticsRuntimeDto Runtime);

