namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsPathsDto(
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);

public sealed record DiagnosticsDto(
    DiagnosticsPathsDto Paths,
    string RuntimeMode,
    string LocalApiVersion,
    RuntimeStatusDto AiRuntimeStatus,
    int PendingIngestionJobsCount);