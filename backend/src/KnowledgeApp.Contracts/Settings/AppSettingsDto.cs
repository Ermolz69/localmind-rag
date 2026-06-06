namespace KnowledgeApp.Contracts.Settings;

/// <summary>All user-configurable LocalMind settings.</summary>
/// <param name="Appearance">Appearance settings.</param>
/// <param name="Ai">AI provider and model settings.</param>
/// <param name="RuntimePaths">Local runtime storage paths.</param>
/// <param name="Sync">Remote sync settings.</param>
/// <param name="WatchedFolders">Watched folder auto-ingestion settings.</param>
public sealed record AppSettingsDto(
    AppearanceSettingsDto Appearance,
    AiSettingsDto Ai,
    RuntimePathsSettingsDto RuntimePaths,
    SyncSettingsDto Sync,
    WatchedFoldersSettingsDto? WatchedFolders = null);
