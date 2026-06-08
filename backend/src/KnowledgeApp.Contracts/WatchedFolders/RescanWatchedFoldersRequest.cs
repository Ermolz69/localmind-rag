namespace KnowledgeApp.Contracts.WatchedFolders;

public sealed record RescanWatchedFoldersRequest(
    string? Path = null);
