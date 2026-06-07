namespace KnowledgeApp.Contracts.Settings;

/// <summary>Single watched folder configuration.</summary>
/// <param name="Path">Absolute folder path to watch.</param>
/// <param name="Enabled">Whether this watched folder is enabled.</param>
/// <param name="IncludeSubdirectories">Whether nested folders should also be watched.</param>
public sealed record WatchedFolderDto(
    string Path,
    bool Enabled,
    bool IncludeSubdirectories);
