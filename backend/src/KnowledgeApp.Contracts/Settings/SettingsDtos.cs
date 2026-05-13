namespace KnowledgeApp.Contracts.Settings;

public sealed record AppSettingsDto(
    AppearanceSettingsDto Appearance,
    AiSettingsDto Ai,
    RuntimePathsSettingsDto RuntimePaths,
    SyncSettingsDto Sync);

public sealed record AppearanceSettingsDto(string Theme);

public sealed record AiSettingsDto(
    string Provider,
    string ChatModel,
    string EmbeddingModel,
    string RuntimePath,
    string ModelsPath);

public sealed record RuntimePathsSettingsDto(
    string DataPath,
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);

public sealed record SyncSettingsDto(bool Enabled, bool AutoSync);
