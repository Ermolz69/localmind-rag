using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services.WatchedFolders;

public sealed class WindowsFileWatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IFileWatcherDebounceBuffer debounceBuffer,
    IWatchedFolderStatusStore statusStore,
    IDateTimeProvider dateTimeProvider,
    ILogger<WindowsFileWatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SettingsReloadInterval = TimeSpan.FromSeconds(5);

    private readonly Dictionary<string, WatcherRegistration> watchers = new(PathComparer);

    private DateTimeOffset lastSettingsReloadAt = DateTimeOffset.MinValue;
    private WatchedFoldersSettingsDto currentSettings = new(
        Enabled: false,
        DebounceMilliseconds: 1000,
        DeletePolicy: "MarkDeleted",
        Folders: []);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReloadSettingsIfNeededAsync(stoppingToken);
                await ProcessReadyChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Watched folder service loop failed.");

                statusStore.RecordGlobalError(
                    "Watcher error. Check watched folder settings and permissions.",
                    dateTimeProvider.UtcNow);
            }

            await Task.Delay(LoopDelay, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        DisposeWatchers();

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        DisposeWatchers();

        base.Dispose();
    }

    private async Task ReloadSettingsIfNeededAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;

        if (now - lastSettingsReloadAt < SettingsReloadInterval)
        {
            return;
        }

        lastSettingsReloadAt = now;

        using IServiceScope scope = scopeFactory.CreateScope();

        ISettingsService settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

        currentSettings = settings.WatchedFolders;

        ApplyWatcherSettings(currentSettings);
    }

    private void ApplyWatcherSettings(WatchedFoldersSettingsDto settings)
    {
        if (!settings.Enabled)
        {
            DisposeWatchers();
            statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
            return;
        }

        HashSet<string> desiredFolderKeys = new(PathComparer);

        foreach (WatchedFolderDto folder in settings.Folders)
        {
            string? folderKey = TryNormalizePath(folder.Path);

            if (folderKey is null)
            {
                statusStore.SetFolderWatching(folder.Path, isWatching: false);
                statusStore.RecordFolderError(
                    folder.Path,
                    "Unable to watch folder. Check that the folder path is valid.",
                    dateTimeProvider.UtcNow);
                continue;
            }

            if (!folder.Enabled)
            {
                desiredFolderKeys.Add(folderKey);
                StopWatcher(folderKey);
                statusStore.SetFolderWatching(folder.Path, isWatching: false);
                continue;
            }

            desiredFolderKeys.Add(folderKey);

            if (watchers.ContainsKey(folderKey))
            {
                statusStore.SetFolderWatching(folder.Path, isWatching: true);
                continue;
            }

            TryStartWatcher(folder, folderKey);
        }

        string[] staleWatcherKeys = watchers.Keys
            .Where(existingKey => !desiredFolderKeys.Contains(existingKey))
            .ToArray();

        foreach (string staleWatcherKey in staleWatcherKeys)
        {
            StopWatcher(staleWatcherKey);
        }

        statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
    }

    private void TryStartWatcher(WatchedFolderDto folder, string folderKey)
    {
        if (!Directory.Exists(folder.Path))
        {
            statusStore.SetFolderWatching(folder.Path, isWatching: false);
            statusStore.RecordFolderError(
                folder.Path,
                "Unable to watch folder. Check that the folder exists and permissions are available.",
                dateTimeProvider.UtcNow);
            return;
        }

        try
        {
            FileSystemWatcher watcher = new FileSystemWatcher(folder.Path)
            {
                IncludeSubdirectories = folder.IncludeSubdirectories,
                Filter = "*.*",
                NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.DirectoryName |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size |
                    NotifyFilters.CreationTime
            };

            watcher.Created += (_, args) => EnqueueCreatedOrChanged(folder.Path, args.FullPath);
            watcher.Changed += (_, args) => EnqueueCreatedOrChanged(folder.Path, args.FullPath);
            watcher.Deleted += (_, args) => EnqueueDeleted(folder.Path, args.FullPath);
            watcher.Renamed += (_, args) =>
            {
                EnqueueDeleted(folder.Path, args.OldFullPath);
                EnqueueCreatedOrChanged(folder.Path, args.FullPath);
            };
            watcher.Error += (_, args) =>
            {
                logger.LogWarning(args.GetException(), "Watched folder FileSystemWatcher error.");

                statusStore.RecordFolderError(
                    folder.Path,
                    "Unable to watch folder. Check folder permissions and operating system watcher limits.",
                    dateTimeProvider.UtcNow);
            };

            watcher.EnableRaisingEvents = true;

            watchers[folderKey] = new WatcherRegistration(folder.Path, watcher);

            statusStore.SetFolderWatching(folder.Path, isWatching: true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to start watched folder watcher.");

            statusStore.SetFolderWatching(folder.Path, isWatching: false);
            statusStore.RecordFolderError(
                folder.Path,
                "Unable to watch folder. Check that the folder exists and permissions are available.",
                dateTimeProvider.UtcNow);
        }
    }

    private void EnqueueCreatedOrChanged(string watchedFolderPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;

        debounceBuffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        statusStore.RecordFolderEvent(watchedFolderPath, now);
        statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
    }

    private void EnqueueDeleted(string watchedFolderPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;

        debounceBuffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.Deleted,
            LastEventAt: now));

        statusStore.RecordFolderEvent(watchedFolderPath, now);
        statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
    }

    private async Task ProcessReadyChangesAsync(CancellationToken cancellationToken)
    {
        TimeSpan debounceDelay = TimeSpan.FromMilliseconds(
            Math.Max(0, currentSettings.DebounceMilliseconds));

        IReadOnlyList<WatchedFileChange> readyChanges = debounceBuffer.DequeueReadyChanges(
            dateTimeProvider.UtcNow,
            debounceDelay);

        if (readyChanges.Count == 0)
        {
            statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();

        IWatchedFileIngestionService watchedFileIngestionService =
            scope.ServiceProvider.GetRequiredService<IWatchedFileIngestionService>();

        foreach (WatchedFileChange change in readyChanges)
        {
            try
            {
                if (change.ChangeType == WatchedFileChangeType.Deleted)
                {
                    await watchedFileIngestionService.HandleDeletedAsync(change.FilePath, cancellationToken);
                }
                else
                {
                    await watchedFileIngestionService.HandleCreatedOrChangedAsync(
                        change.FilePath,
                        change.WatchedFolderPath,
                        cancellationToken);
                }

                statusStore.SetFolderPendingEvents(change.WatchedFolderPath, pendingEvents: 0);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to process watched file change.");

                statusStore.RecordFolderError(
                    change.WatchedFolderPath,
                    "Unable to process watched file change.",
                    dateTimeProvider.UtcNow);
            }
        }

        statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
    }

    private void StopWatcher(string folderKey)
    {
        if (!watchers.Remove(folderKey, out WatcherRegistration? registration))
        {
            return;
        }

        statusStore.SetFolderWatching(registration.FolderPath, isWatching: false);
        registration.Watcher.Dispose();
    }

    private void DisposeWatchers()
    {
        foreach (string watcherKey in watchers.Keys.ToArray())
        {
            StopWatcher(watcherKey);
        }
    }

    private static string? TryNormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            string fullPath = Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return OperatingSystem.IsWindows()
                ? fullPath.ToUpperInvariant()
                : fullPath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private sealed record WatcherRegistration(
        string FolderPath,
        FileSystemWatcher Watcher);
}
