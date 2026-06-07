using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFolderReconciliationService
{
    Task ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default);
}
