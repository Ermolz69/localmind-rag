using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFolderStatusStore
{
    WatchedFolderStatusResponse GetStatus(WatchedFoldersSettingsDto settings);

    void SetFolderWatching(string folderPath, bool isWatching);

    void SetFolderPendingEvents(string folderPath, int pendingEvents);

    void RecordFolderEvent(string folderPath, DateTimeOffset occurredAt);

    void RecordFolderError(string folderPath, string sanitizedError, DateTimeOffset occurredAt);

    void RecordGlobalError(string sanitizedError, DateTimeOffset occurredAt);

    void SetGlobalPendingEvents(int pendingEvents);
}
