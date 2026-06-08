namespace KnowledgeApp.Contracts.WatchedFolders;

public static class WatchedFolderHealthStatuses
{
    public const string Disabled = "Disabled";
    public const string Missing = "Missing";
    public const string WatcherError = "WatcherError";
    public const string Scanning = "Scanning";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public sealed record WatchedFolderStatusDto(
    string Path,
    bool Enabled,
    bool IncludeSubdirectories,
    bool Exists,
    bool IsWatching,
    int PendingEvents,
    DateTimeOffset? LastEventAt,
    string? LastError,
    DateTimeOffset? LastErrorAt,
    string HealthStatus,
    DateTimeOffset? LastScanStartedAt,
    DateTimeOffset? LastScanCompletedAt,
    int ActiveDocumentsCount,
    int DeletedWaitingCleanupCount,
    int LastScanNewFiles,
    int LastScanChangedFiles,
    int LastScanDeletedFiles,
    int LastScanUnchangedFiles,
    int LastScanUnsupportedFiles);
