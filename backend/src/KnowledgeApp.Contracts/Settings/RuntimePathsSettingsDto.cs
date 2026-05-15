namespace KnowledgeApp.Contracts.Settings;

public sealed record RuntimePathsSettingsDto(
    string DataPath,
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);


