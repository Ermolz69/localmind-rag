using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Commands;

public sealed class CleanupWatchedFoldersHandler(IWatchedFolderCleanupService cleanupService)
{
    public async Task<Result<CleanupWatchedFoldersResult>> HandleAsync(CleanupWatchedFoldersCommand command, CancellationToken cancellationToken = default)
    {
        int count = await cleanupService.CleanupDeletedFilesAsync(cancellationToken);
        return Result<CleanupWatchedFoldersResult>.Success(new CleanupWatchedFoldersResult(count));
    }
}
