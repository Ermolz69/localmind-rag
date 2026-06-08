namespace KnowledgeApp.Contracts.Settings;

/// <summary>Watched folder settings for automatic ingestion.</summary>
/// <param name="Enabled">Whether watched folder auto-ingestion is enabled.</param>
/// <param name="DebounceMilliseconds">Delay used to collapse rapid file events.</param>
/// <param name="DeletePolicy">Policy used when a watched file is deleted.</param>
/// <param name="Folders">Configured watched folders.</param>
/// <param name="IgnoredFolders">Folders to ignore.</param>
/// <param name="IgnoredPatterns">Filename patterns to ignore.</param>
/// <param name="MaxFileSizeMb">Maximum file size in MB.</param>
/// <param name="AllowedExtensions">Allowed extensions (null means all supported types).</param>
/// <param name="StorageMode">How to store watched files (LinkOnly or CopyToAppStorage).</param>
public sealed record WatchedFoldersSettingsDto(
    bool Enabled,
    int DebounceMilliseconds,
    string DeletePolicy,
    IReadOnlyList<WatchedFolderDto> Folders,
    IReadOnlyList<string>? IgnoredFolders = null,
    IReadOnlyList<string>? IgnoredPatterns = null,
    int? MaxFileSizeMb = null,
    IReadOnlyList<string>? AllowedExtensions = null,
    string StorageMode = WatchedFolderStorageModes.LinkOnly);
