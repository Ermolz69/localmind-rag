namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IFileWatcherDebounceBuffer
{
    int PendingCount { get; }

    void AddOrUpdate(WatchedFileChange change);

    IReadOnlyList<WatchedFileChange> DequeueReadyChanges(
        DateTimeOffset now,
        TimeSpan debounceDelay);
}
