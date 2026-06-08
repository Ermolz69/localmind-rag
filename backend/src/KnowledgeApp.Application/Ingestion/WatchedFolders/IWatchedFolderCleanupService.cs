namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFolderCleanupService
{
    Task<int> CleanupDeletedFilesAsync(CancellationToken cancellationToken = default);
}
