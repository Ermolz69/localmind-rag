namespace KnowledgeApp.Contracts.WatchedFolders;

public sealed record WatchedFolderStatusDto(
    string Path,
    bool Enabled,
    bool IncludeSubdirectories,
    bool Exists,
    bool IsWatching,
    int PendingEvents,
    DateTimeOffset? LastEventAt,
    string? LastError,
    DateTimeOffset? LastErrorAt);
