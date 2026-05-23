namespace KnowledgeApp.Contracts.Settings;

/// <summary>Local runtime storage path settings.</summary>
/// <param name="DataPath">Root data directory.</param>
/// <param name="DatabasePath">SQLite database path.</param>
/// <param name="FilesPath">Managed local files directory.</param>
/// <param name="IndexPath">Vector index directory.</param>
/// <param name="LogsPath">Local logs directory.</param>
public sealed record RuntimePathsSettingsDto(
    string DataPath,
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);

