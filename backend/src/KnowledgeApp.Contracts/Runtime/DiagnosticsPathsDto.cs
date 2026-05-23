namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Resolved local runtime paths.</summary>
public sealed record DiagnosticsPathsDto(
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);

