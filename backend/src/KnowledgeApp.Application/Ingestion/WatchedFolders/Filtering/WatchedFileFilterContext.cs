using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;

public sealed class WatchedFileFilterContext
{
    public WatchedFoldersSettingsDto Settings { get; }
    public HashSet<string> IgnoredFolders { get; }
    public IReadOnlyList<string> IgnoredPatterns { get; }
    public HashSet<string> AllowedExtensions { get; }
    public long? MaxFileSizeBytes { get; }

    public WatchedFileFilterContext(WatchedFoldersSettingsDto settings)
    {
        Settings = settings;

        StringComparer pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        IgnoredFolders = settings.IgnoredFolders is not null
            ? new HashSet<string>(settings.IgnoredFolders, pathComparer)
            : new HashSet<string>(pathComparer);

        IgnoredPatterns = settings.IgnoredPatterns ?? Array.Empty<string>();

        AllowedExtensions = settings.AllowedExtensions is not null
            ? new HashSet<string>(
                settings.AllowedExtensions.Select(ext => ext.StartsWith('.') ? ext : $".{ext}"),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        MaxFileSizeBytes = settings.MaxFileSizeMb.HasValue
            ? settings.MaxFileSizeMb.Value * 1024L * 1024L
            : null;
    }
}
