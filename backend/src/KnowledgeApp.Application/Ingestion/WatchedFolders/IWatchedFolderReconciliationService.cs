using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public sealed record WatchedFolderReconciliationResult(
    int QueuedCreatedOrChanged,
    int QueuedDeleted,
    int UnchangedFiles,
    int UnsupportedFiles);

public interface IWatchedFolderReconciliationService
{
    Task<WatchedFolderReconciliationResult> ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default);
}
