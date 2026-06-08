using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;
using KnowledgeApp.Application.Settings;
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
    ISettingsService settingsService,
    IWatchedFileFilterService filterService,
    ILogger<WatchedFolderReconciliationService> logger) : IWatchedFolderReconciliationService
{
    public async Task<WatchedFolderReconciliationResult> ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default)
    {
        int queuedCreatedOrChanged = 0;
        int queuedDeleted = 0;
        int unchangedFiles = 0;
        int unsupportedFiles = 0;

        if (string.IsNullOrWhiteSpace(folder.Path) || !Directory.Exists(folder.Path))
        {
            return new WatchedFolderReconciliationResult(0, 0, 0, 0);
        }

        try
        {
            AppSettingsDto appSettings = await settingsService.GetAsync(cancellationToken);
            WatchedFoldersSettingsDto settings = appSettings.WatchedFolders ?? new WatchedFoldersSettingsDto(
                Enabled: false, DebounceMilliseconds: 1000, DeletePolicy: "MarkDeleted", Folders: []);
            WatchedFileFilterContext filterContext = filterService.CreateContext(settings);

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
                var allFiles = Directory.EnumerateFiles(folder.Path, "*.*", options).ToList();
                diskFiles = new List<string>();

                foreach (var file in allFiles)
                {
                    WatchedFileFilterResult result = filterService.Evaluate(file, filterContext);
                    if (result.IsAllowed)
                    {
                        diskFiles.Add(file);
                    }
                    else
                    {
                        unsupportedFiles++;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                logger.LogWarning(ex, "Watched folder became inaccessible during reconciliation: {FolderPath}", folder.Path);
                return new WatchedFolderReconciliationResult(0, 0, 0, 0);
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
                    queuedCreatedOrChanged++;
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
                        queuedCreatedOrChanged++;
                    }
                    else
                    {
                        unchangedFiles++;
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
                    queuedDeleted++;
                }
            }

            return new WatchedFolderReconciliationResult(
                QueuedCreatedOrChanged: queuedCreatedOrChanged,
                QueuedDeleted: queuedDeleted,
                UnchangedFiles: unchangedFiles,
                UnsupportedFiles: unsupportedFiles);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to perform startup reconciliation scan for watched folder {FolderPath}.", folder.Path);
            return new WatchedFolderReconciliationResult(queuedCreatedOrChanged, queuedDeleted, unchangedFiles, unsupportedFiles);
        }
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
