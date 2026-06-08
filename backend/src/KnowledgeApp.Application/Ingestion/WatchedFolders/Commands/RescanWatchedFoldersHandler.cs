using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Commands;

public sealed class RescanWatchedFoldersHandler(
    ISettingsService settingsService,
    IWatchedFolderReconciliationService reconciliationService,
    IWatchedFolderStatusStore statusStore,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RescanWatchedFoldersCommand, Result<RescanWatchedFoldersResponse>>
{
    public async Task<Result<RescanWatchedFoldersResponse>> Handle(RescanWatchedFoldersCommand request, CancellationToken cancellationToken)
    {
        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

        WatchedFoldersSettingsDto watchedFolders = settings.WatchedFolders ?? new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);

        int scannedFolders = 0;
        int failedFolders = 0;
        int queuedCreatedOrChanged = 0;
        int queuedDeleted = 0;
        int unchangedFiles = 0;
        int unsupportedFiles = 0;

        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            // Rescan specific folder
            WatchedFolderDto? folder = watchedFolders.Folders
                .FirstOrDefault(f => string.Equals(f.Path, request.Path, StringComparison.OrdinalIgnoreCase));

            if (folder is null || !folder.Enabled)
            {
                return Result<RescanWatchedFoldersResponse>.Failure(
                    ApplicationErrors.Validation(
                        "FOLDER_DISABLED",
                        "Folder is disabled or not configured."));
            }

            if (!Directory.Exists(folder.Path))
            {
                return Result<RescanWatchedFoldersResponse>.Failure(ApplicationErrors.Validation(
                    "FOLDER_MISSING",
                    "Folder is unavailable or missing."));
            }

            var result = await PerformScanAsync(folder, cancellationToken);

            scannedFolders = 1;
            queuedCreatedOrChanged = result.QueuedCreatedOrChanged;
            queuedDeleted = result.QueuedDeleted;
            unchangedFiles = result.UnchangedFiles;
            unsupportedFiles = result.UnsupportedFiles;
        }
        else
        {
            // Rescan all enabled folders
            foreach (WatchedFolderDto folder in watchedFolders.Folders.Where(f => f.Enabled))
            {
                if (!Directory.Exists(folder.Path))
                {
                    failedFolders++;
                    continue;
                }

                var result = await PerformScanAsync(folder, cancellationToken);

                scannedFolders++;
                queuedCreatedOrChanged += result.QueuedCreatedOrChanged;
                queuedDeleted += result.QueuedDeleted;
                unchangedFiles += result.UnchangedFiles;
                unsupportedFiles += result.UnsupportedFiles;
            }
        }

        return Result<RescanWatchedFoldersResponse>.Success(new RescanWatchedFoldersResponse(
            ScannedFolders: scannedFolders,
            QueuedCreatedOrChanged: queuedCreatedOrChanged,
            QueuedDeleted: queuedDeleted,
            UnchangedFiles: unchangedFiles,
            UnsupportedFiles: unsupportedFiles,
            FailedFolders: failedFolders));
    }

    private async Task<WatchedFolderReconciliationResult> PerformScanAsync(WatchedFolderDto folder, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = dateTimeProvider.UtcNow;
        statusStore.RecordScanStarted(folder.Path, startedAt);

        var result = await reconciliationService.ReconcileFolderAsync(folder, cancellationToken);

        DateTimeOffset completedAt = dateTimeProvider.UtcNow;
        statusStore.RecordScanCompleted(folder.Path, completedAt, result);

        return result;
    }
}
