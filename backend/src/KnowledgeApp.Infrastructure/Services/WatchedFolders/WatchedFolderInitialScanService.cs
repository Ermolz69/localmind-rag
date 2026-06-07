using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services.WatchedFolders;

public sealed class WatchedFolderInitialScanService(
    IFileWatcherDebounceBuffer debounceBuffer,
    IDateTimeProvider dateTimeProvider,
    ILogger<WatchedFolderInitialScanService> logger) : IWatchedFolderInitialScanService
{
    public void EnqueueInitialFiles(WatchedFolderDto folder)
    {
        if (string.IsNullOrWhiteSpace(folder.Path))
        {
            return;
        }

        try
        {
            EnumerationOptions options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = folder.IncludeSubdirectories
            };

            Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(folder.Path))
                    {
                        return;
                    }

                    foreach (string filePath in Directory.EnumerateFiles(folder.Path, "*.*", options))
                    {
                        if (IsSupportedFile(filePath))
                        {
                            debounceBuffer.AddOrUpdate(new WatchedFileChange(
                                FilePath: filePath,
                                WatchedFolderPath: folder.Path,
                                ChangeType: WatchedFileChangeType.CreatedOrChanged,
                                LastEventAt: dateTimeProvider.UtcNow));
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to perform initial scan for watched folder '{FolderPath}'.", folder.Path);
                }
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to start initial scan for watched folder '{FolderPath}'.", folder.Path);
        }
    }

    private static bool IsSupportedFile(string filePath)
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
}
