namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsDto(
    DiagnosticsPathsDto Paths,
    DiagnosticsStorageDto Storage,
    DiagnosticsCountsDto Counts,
    IReadOnlyList<DiagnosticsIngestionErrorDto> LatestErrors,
    DiagnosticsRuntimeDto Runtime);


