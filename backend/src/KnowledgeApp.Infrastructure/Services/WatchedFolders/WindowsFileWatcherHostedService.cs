using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;
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
    IWatchedFileFilterService filterService,
    ISettingsChangeSignal settingsChangeSignal,
    KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager processManager,
    ILogger<WindowsFileWatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromMilliseconds(250);

    private readonly Dictionary<string, WatcherRegistration> watchers = new(PathComparer);
    private readonly HashSet<string> startupReconciledFolderKeys = new(PathComparer);

    private WatchedFoldersSettingsDto currentSettings = new(
        Enabled: false,
        DebounceMilliseconds: 1000,
        DeletePolicy: "MarkDeleted",
        Folders: []);

    private WatchedFileFilterContext? currentFilterContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(LoopDelay);
        Task<bool> tickTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
        Task settingsChangedTask = settingsChangeSignal.ReadAsync(stoppingToken).AsTask();

        try
        {
            await ReloadSettingsSafelyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested && !processManager.IsShuttingDown)
            {
                Task completedTask = await Task.WhenAny(tickTask, settingsChangedTask);

                if (completedTask == settingsChangedTask)
                {
                    await settingsChangedTask;
                    await ReloadSettingsSafelyAsync(stoppingToken);
                    settingsChangedTask = settingsChangeSignal.ReadAsync(stoppingToken).AsTask();
                }

                if (completedTask == tickTask)
                {
                    if (!await tickTask)
                    {
                        break;
                    }

                    await ProcessReadyChangesSafelyAsync(stoppingToken);
                    tickTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
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

    private async Task ReloadSettingsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        ISettingsService settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

        currentSettings = settings.WatchedFolders ?? CreateDisabledWatchedFolderSettings();
        currentFilterContext = filterService.CreateContext(currentSettings);

        IReadOnlyList<StartupReconciliationRequest> foldersToReconcile =
            ApplyWatcherSettings(currentSettings);

        await RunStartupReconciliationAsync(foldersToReconcile, cancellationToken);
    }

    private async Task ReloadSettingsSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ReloadSettingsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordLoopFailure(exception);
        }
    }

    private async Task ProcessReadyChangesSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProcessReadyChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordLoopFailure(exception);
        }
    }

    private void RecordLoopFailure(Exception exception)
    {
        logger.LogWarning(exception, "Watched folder service loop failed.");

        statusStore.RecordGlobalError(
            "Watcher error. Check watched folder settings and permissions.",
            dateTimeProvider.UtcNow);
    }

    private IReadOnlyList<StartupReconciliationRequest> ApplyWatcherSettings(WatchedFoldersSettingsDto settings)
    {
        List<StartupReconciliationRequest> foldersToReconcile = [];

        if (!settings.Enabled)
        {
            DisposeWatchers();
            startupReconciledFolderKeys.Clear();
            statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);

            return foldersToReconcile;
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

                if (!startupReconciledFolderKeys.Contains(folderKey))
                {
                    foldersToReconcile.Add(new StartupReconciliationRequest(folder, folderKey));
                }

                continue;
            }

            if (TryStartWatcher(folder, folderKey))
            {
                foldersToReconcile.Add(new StartupReconciliationRequest(folder, folderKey));
            }
        }

        string[] staleWatcherKeys = watchers.Keys
            .Where(existingKey => !desiredFolderKeys.Contains(existingKey))
            .ToArray();

        foreach (string staleWatcherKey in staleWatcherKeys)
        {
            StopWatcher(staleWatcherKey);
        }

        statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);

        return foldersToReconcile;
    }

    private bool TryStartWatcher(WatchedFolderDto folder, string folderKey)
    {
        if (!Directory.Exists(folder.Path))
        {
            statusStore.SetFolderWatching(folder.Path, isWatching: false);

            statusStore.RecordFolderError(
                folder.Path,
                "Unable to watch folder. Check that the folder exists and permissions are available.",
                dateTimeProvider.UtcNow);

            return false;
        }

        try
        {
            FileSystemWatcher watcher = new FileSystemWatcher(folder.Path)
            {
                IncludeSubdirectories = folder.IncludeSubdirectories,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
                    | NotifyFilters.CreationTime
            };

            watcher.Created += (_, args) => EnqueueCreatedOrChanged(folder.Path, args.FullPath);
            watcher.Changed += (_, args) => EnqueueCreatedOrChanged(folder.Path, args.FullPath);
            watcher.Deleted += (_, args) => EnqueueDeleted(folder.Path, args.FullPath);
            watcher.Renamed += (_, args) => EnqueueRenamed(folder.Path, args.OldFullPath, args.FullPath);

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

            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to start watched folder watcher.");

            statusStore.SetFolderWatching(folder.Path, isWatching: false);

            statusStore.RecordFolderError(
                folder.Path,
                "Unable to watch folder. Check that the folder exists and permissions are available.",
                dateTimeProvider.UtcNow);

            return false;
        }
    }

    private async Task RunStartupReconciliationAsync(
        IReadOnlyList<StartupReconciliationRequest> foldersToReconcile,
        CancellationToken cancellationToken)
    {
        if (foldersToReconcile.Count == 0)
        {
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();

        IWatchedFolderReconciliationService reconciliationService =
            scope.ServiceProvider.GetRequiredService<IWatchedFolderReconciliationService>();

        foreach (StartupReconciliationRequest request in foldersToReconcile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTimeOffset startedAt = dateTimeProvider.UtcNow;
            statusStore.RecordScanStarted(request.Folder.Path, startedAt);

            try
            {
                WatchedFolderReconciliationResult result =
                    await reconciliationService.ReconcileFolderAsync(request.Folder, cancellationToken);

                DateTimeOffset completedAt = dateTimeProvider.UtcNow;
                statusStore.RecordScanCompleted(request.Folder.Path, completedAt, result);

                startupReconciledFolderKeys.Add(request.FolderKey);

                statusStore.SetGlobalPendingEvents(debounceBuffer.PendingCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to perform startup reconciliation scan for watched folder {FolderPath}.",
                    request.Folder.Path);

                statusStore.RecordFolderError(
                    request.Folder.Path,
                    "Unable to scan watched folder on startup.",
                    dateTimeProvider.UtcNow);
            }
        }
    }

    private void EnqueueCreatedOrChanged(string watchedFolderPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (currentFilterContext is not null)
        {
            WatchedFileFilterResult result = filterService.Evaluate(filePath, currentFilterContext);

            if (!result.IsAllowed)
            {
                return;
            }
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

    private void EnqueueRenamed(string watchedFolderPath, string oldFilePath, string newFilePath)
    {
        if (string.IsNullOrWhiteSpace(oldFilePath) || string.IsNullOrWhiteSpace(newFilePath))
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;

        debounceBuffer.AddOrUpdate(new WatchedFileChange(
            FilePath: newFilePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.Renamed,
            LastEventAt: now,
            OldFilePath: oldFilePath));

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
                else if (change.ChangeType == WatchedFileChangeType.Renamed && change.OldFilePath is not null)
                {
                    await watchedFileIngestionService.HandleRenamedAsync(
                        change.OldFilePath,
                        change.FilePath,
                        change.WatchedFolderPath,
                        cancellationToken);
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
        startupReconciledFolderKeys.Remove(folderKey);

        if (!watchers.Remove(folderKey, out WatcherRegistration? registration))
        {
            return;
        }

        statusStore.SetFolderWatching(registration.FolderPath, isWatching: false);

        try
        {
            registration.Watcher.EnableRaisingEvents = false;
        }
        catch (ObjectDisposedException) { }

        registration.Watcher.Dispose();
    }

    private void DisposeWatchers()
    {
        foreach (string watcherKey in watchers.Keys.ToArray())
        {
            StopWatcher(watcherKey);
        }

        startupReconciledFolderKeys.Clear();
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

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static WatchedFoldersSettingsDto CreateDisabledWatchedFolderSettings()
    {
        return new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);
    }

    private sealed record StartupReconciliationRequest(
        WatchedFolderDto Folder,
        string FolderKey);

    private sealed record WatcherRegistration(
        string FolderPath,
        FileSystemWatcher Watcher);
}
