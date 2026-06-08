namespace KnowledgeApp.Contracts.WatchedFolders;

public sealed record RescanWatchedFoldersResponse(
    int ScannedFolders,
    int QueuedCreatedOrChanged,
    int QueuedDeleted,
    int UnchangedFiles,
    int UnsupportedFiles,
    int FailedFolders);
