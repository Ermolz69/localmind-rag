namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public sealed record WatchedFileChange(
    string FilePath,
    string WatchedFolderPath,
    WatchedFileChangeType ChangeType,
    DateTimeOffset LastEventAt,
    string? OldFilePath = null);
