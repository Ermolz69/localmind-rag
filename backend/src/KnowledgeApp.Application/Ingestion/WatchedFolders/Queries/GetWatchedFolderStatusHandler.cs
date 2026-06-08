using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Queries;

public sealed class GetWatchedFolderStatusHandler(
    ISettingsService settingsService,
    IWatchedFolderStatusStore statusStore,
    IAppDbContext dbContext)
    : IRequestHandler<GetWatchedFolderStatusQuery, WatchedFolderStatusResponse>
{
    public async Task<WatchedFolderStatusResponse> Handle(GetWatchedFolderStatusQuery request, CancellationToken cancellationToken)
    {
        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

        WatchedFoldersSettingsDto watchedFolders = settings.WatchedFolders ?? new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);

        WatchedFolderStatusResponse baseStatus = statusStore.GetStatus(watchedFolders);

        var updatedFolders = new List<WatchedFolderStatusDto>();

        foreach (var folder in baseStatus.Folders)
        {
            int activeCount = 0;
            int deletedWaitingCount = 0;

            if (folder.Enabled && folder.Exists)
            {
                string normalizedPath = NormalizePath(folder.Path);

                var query = dbContext.WatchedFileLinks
                    .Where(link => link.NormalizedWatchedFolderPath == normalizedPath)
                    .Join(dbContext.Documents,
                        link => link.DocumentId,
                        doc => doc.Id,
                        (link, doc) => doc.Status);

                var counts = await query
                    .GroupBy(status => status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.Status, v => v.Count, cancellationToken);

                deletedWaitingCount = counts.GetValueOrDefault(DocumentStatus.Deleted, 0);
                activeCount = counts.Where(kv => kv.Key != DocumentStatus.Deleted).Sum(kv => kv.Value);
            }

            string healthStatus = CalculateHealthStatus(folder, baseStatus.Enabled);

            updatedFolders.Add(folder with
            {
                ActiveDocumentsCount = activeCount,
                DeletedWaitingCleanupCount = deletedWaitingCount,
                HealthStatus = healthStatus
            });
        }

        return baseStatus with { Folders = updatedFolders };
    }

    private static string CalculateHealthStatus(WatchedFolderStatusDto folder, bool globalEnabled)
    {
        if (!globalEnabled || !folder.Enabled)
        {
            return WatchedFolderHealthStatuses.Disabled;
        }

        if (!folder.Exists)
        {
            return WatchedFolderHealthStatuses.Missing;
        }

        if (folder.LastError != null)
        {
            return WatchedFolderHealthStatuses.WatcherError;
        }

        // Technically "Scanning" could be deduced if LastScanStartedAt > LastScanCompletedAt, but we don't strictly have a scanning flag. 
        // We can infer scanning if LastScanStartedAt has a value but completed at doesn't, or if completed is older.
        if (folder.LastScanStartedAt.HasValue &&
            (!folder.LastScanCompletedAt.HasValue || folder.LastScanStartedAt > folder.LastScanCompletedAt))
        {
            return WatchedFolderHealthStatuses.Scanning;
        }

        if (folder.IsWatching)
        {
            return WatchedFolderHealthStatuses.Active;
        }

        return WatchedFolderHealthStatuses.Inactive;
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
