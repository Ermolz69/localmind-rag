namespace KnowledgeApp.Contracts.Settings;

/// <summary>Watched folder settings for automatic ingestion.</summary>
/// <param name="Enabled">Whether watched folder auto-ingestion is enabled.</param>
/// <param name="DebounceMilliseconds">Delay used to collapse rapid file events.</param>
/// <param name="DeletePolicy">Policy used when a watched file is deleted.</param>
/// <param name="Folders">Configured watched folders.</param>
public sealed record WatchedFoldersSettingsDto(
    bool Enabled,
    int DebounceMilliseconds,
    string DeletePolicy,
    IReadOnlyList<WatchedFolderDto> Folders);
