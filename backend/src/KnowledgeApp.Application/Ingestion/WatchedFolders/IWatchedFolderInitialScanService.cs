using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFolderInitialScanService
{
    void EnqueueInitialFiles(WatchedFolderDto folder);
}
