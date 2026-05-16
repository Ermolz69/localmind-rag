namespace KnowledgeApp.Contracts.Settings;

public sealed record AppSettingsDto(
    AppearanceSettingsDto Appearance,
    AiSettingsDto Ai,
    RuntimePathsSettingsDto RuntimePaths,
    SyncSettingsDto Sync);


