namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsPathsDto(
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);


