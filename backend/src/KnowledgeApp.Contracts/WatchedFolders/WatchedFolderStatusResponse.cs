namespace KnowledgeApp.Contracts.WatchedFolders;

public sealed record WatchedFolderStatusResponse(
    bool Enabled,
    int DebounceMilliseconds,
    int PendingEvents,
    string DeletePolicy,
    string? LastError,
    DateTimeOffset? LastErrorAt,
    IReadOnlyList<WatchedFolderStatusDto> Folders);
