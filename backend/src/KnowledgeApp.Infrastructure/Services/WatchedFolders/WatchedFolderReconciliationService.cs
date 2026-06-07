using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services.WatchedFolders;

public sealed class WatchedFolderReconciliationService(
    IServiceScopeFactory scopeFactory,
    IFileWatcherDebounceBuffer debounceBuffer,
    IDateTimeProvider dateTimeProvider,
    ILogger<WatchedFolderReconciliationService> logger) : IWatchedFolderReconciliationService
{
    public async Task ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folder.Path) || !Directory.Exists(folder.Path))
        {
            return;
        }

        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string normalizedWatchedFolderPath = NormalizePath(folder.Path);

            Dictionary<string, WatchedFileLink> existingLinks = await dbContext.WatchedFileLinks
                .Where(link => link.NormalizedWatchedFolderPath == normalizedWatchedFolderPath)
                .ToDictionaryAsync(link => link.NormalizedFilePath, cancellationToken);

            EnumerationOptions options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = folder.IncludeSubdirectories
            };

            List<string> diskFiles;
            try
            {
                diskFiles = Directory.EnumerateFiles(folder.Path, "*.*", options)
                    .Where(IsSupportedFileType)
                    .ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                logger.LogWarning(ex, "Watched folder became inaccessible during reconciliation: {FolderPath}", folder.Path);
                return;
            }

            DateTimeOffset now = dateTimeProvider.UtcNow;

            foreach (string filePath in diskFiles)
            {
                string normalizedFilePath = NormalizePath(filePath);

                if (!existingLinks.TryGetValue(normalizedFilePath, out WatchedFileLink? link))
                {
                    // New file
                    debounceBuffer.AddOrUpdate(new WatchedFileChange(
                        FilePath: filePath,
                        WatchedFolderPath: folder.Path,
                        ChangeType: WatchedFileChangeType.CreatedOrChanged,
                        LastEventAt: now));
                }
                else
                {
                    // Existing file
                    existingLinks.Remove(normalizedFilePath);

                    DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);

                    if (link.LastSeenAt is null || lastWriteTimeUtc > link.LastSeenAt.Value.UtcDateTime)
                    {
                        debounceBuffer.AddOrUpdate(new WatchedFileChange(
                            FilePath: filePath,
                            WatchedFolderPath: folder.Path,
                            ChangeType: WatchedFileChangeType.CreatedOrChanged,
                            LastEventAt: now));
                    }
                }
            }

            foreach (WatchedFileLink deletedLink in existingLinks.Values)
            {
                if (deletedLink.DeletedAt is null)
                {
                    debounceBuffer.AddOrUpdate(new WatchedFileChange(
                        FilePath: deletedLink.FilePath,
                        WatchedFolderPath: folder.Path,
                        ChangeType: WatchedFileChangeType.Deleted,
                        LastEventAt: now));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to perform startup reconciliation scan for watched folder {FolderPath}.", folder.Path);
        }
    }

    private static bool IsSupportedFileType(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => true,
            ".docx" => true,
            ".pptx" => true,
            ".md" => true,
            ".markdown" => true,
            ".txt" => true,
            ".html" => true,
            ".htm" => true,
            _ => false
        };
    }

    private static string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? fullPath.ToUpperInvariant()
            : fullPath;
    }
}
