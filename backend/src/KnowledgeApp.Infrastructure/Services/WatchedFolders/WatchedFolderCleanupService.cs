using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services.WatchedFolders;

public sealed class WatchedFolderCleanupService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService,
    ILogger<WatchedFolderCleanupService> logger) : IWatchedFolderCleanupService
{
    public async Task<int> CleanupDeletedFilesAsync(CancellationToken cancellationToken = default)
    {
        var deletedLinks = await dbContext.WatchedFileLinks
            .Where(link => link.DeletedAt != null)
            .Select(link => new { link.Id, link.DocumentId })
            .ToArrayAsync(cancellationToken);

        if (deletedLinks.Length == 0)
        {
            return 0;
        }

        Guid[] linkIds = deletedLinks.Select(x => x.Id).ToArray();
        Guid[] documentIds = deletedLinks.Select(x => x.DocumentId).ToArray();

        string[] filePathsToDelete = await dbContext.DocumentFiles
            .Where(f => documentIds.Contains(f.DocumentId))
            .Select(f => f.LocalPath)
            .ToArrayAsync(cancellationToken);

        int cleanedCount = 0;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Delete Embeddings for Chunks
            await dbContext.DocumentEmbeddings
                .Where(e => dbContext.DocumentChunks.Any(c => documentIds.Contains(c.DocumentId) && c.Id == e.DocumentChunkId))
                .ExecuteDeleteAsync(cancellationToken);

            // Delete Chunks
            await dbContext.DocumentChunks
                .Where(c => documentIds.Contains(c.DocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            // Delete Document Files (which are just DB records for Watched Folders, we don't delete actual files from user folders)
            await dbContext.DocumentFiles
                .Where(f => documentIds.Contains(f.DocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            // Delete Ingestion Jobs
            await dbContext.IngestionJobs
                .Where(j => documentIds.Contains(j.DocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            // Delete WatchedFileLinks
            cleanedCount = await dbContext.WatchedFileLinks
                .Where(link => linkIds.Contains(link.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // Finally, delete Documents
            await dbContext.Documents
                .Where(d => documentIds.Contains(d.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // Optional: Also cleanup DocumentTags if they exist
            await dbContext.DocumentTags
                .Where(t => documentIds.Contains(t.DocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            // And DocumentChunkTags
            await dbContext.DocumentChunkTags
                .Where(t => dbContext.DocumentChunks.Any(c => documentIds.Contains(c.DocumentId) && c.Id == t.DocumentChunkId))
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            foreach (string filePath in filePathsToDelete)
            {
                try
                {
                    await fileStorageService.DeleteAsync(filePath, cancellationToken);
                }
                catch (Exception fileEx)
                {
                    logger.LogWarning(fileEx, "Failed to delete physical file during cleanup: {FilePath}", filePath);
                }
            }

            logger.LogInformation("Cleaned up {CleanedCount} deleted watched documents", cleanedCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to clean up deleted watched documents");
            throw;
        }

        return cleanedCount;
    }
}
