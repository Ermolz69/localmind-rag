using System.Security.Cryptography;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;
using KnowledgeApp.Contracts.Documents;

namespace KnowledgeApp.Infrastructure.Services.WatchedFolders;

public sealed class WatchedFileIngestionService(
    AppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ISettingsService settingsService,
    IWatchedFileFilterService filterService,
    IFileStorageService fileStorageService) : IWatchedFileIngestionService
{
    private const int ReadRetryCount = 5;
    private static readonly TimeSpan ReadRetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task HandleCreatedOrChangedAsync(
        string filePath,
        string watchedFolderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(watchedFolderPath))
        {
            return;
        }

        FileType fileType = ResolveFileType(filePath);

        if (fileType == FileType.Unknown)
        {
            return;
        }

        AppSettingsDto appSettings = await settingsService.GetAsync(cancellationToken);
        WatchedFoldersSettingsDto settings = appSettings.WatchedFolders ?? new WatchedFoldersSettingsDto(
            Enabled: false, DebounceMilliseconds: 1000, DeletePolicy: "MarkDeleted", Folders: []);

        WatchedFileFilterContext filterContext = filterService.CreateContext(settings);
        WatchedFileFilterResult filterResult = filterService.Evaluate(filePath, filterContext);

        if (!filterResult.IsAllowed)
        {
            return;
        }

        string normalizedFilePath;
        string normalizedWatchedFolderPath;

        try
        {
            normalizedFilePath = NormalizePath(filePath);
            normalizedWatchedFolderPath = NormalizePath(watchedFolderPath);
        }
        catch (Exception)
        {
            return;
        }

        if (!IsFileInsideFolder(normalizedFilePath, normalizedWatchedFolderPath))
        {
            return;
        }

        FileSnapshot? fileSnapshot = await TryReadFileSnapshotAsync(filePath, cancellationToken);

        if (fileSnapshot is null)
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTime nowUtc = now.UtcDateTime;

        WatchedFileLink? existingLink = await dbContext.WatchedFileLinks
            .FirstOrDefaultAsync(link => link.NormalizedFilePath == normalizedFilePath, cancellationToken);

        string localPath = filePath;

        if (existingLink is null)
        {

            if (settings.StorageMode == WatchedFolderStorageModes.CopyToAppStorage)
            {
                await using FileStream contentStream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                Guid documentId = Guid.NewGuid(); // Pre-generate so SaveAsync knows where to put it
                StoredFileDto storedFile = await fileStorageService.SaveAsync(
                    contentStream, documentId, Path.GetFileName(filePath), cancellationToken);
                localPath = storedFile.LocalPath;
                // Use the documentId in CreateDocumentForWatchedFileAsync
                await CreateDocumentForWatchedFileAsync(
                    documentId,
                    filePath,
                    localPath,
                    normalizedFilePath,
                    normalizedWatchedFolderPath,
                    fileSnapshot,
                    fileType,
                    nowUtc,
                    cancellationToken);
            }
            else
            {
                await CreateDocumentForWatchedFileAsync(
                    Guid.NewGuid(),
                    filePath,
                    localPath,
                    normalizedFilePath,
                    normalizedWatchedFolderPath,
                    fileSnapshot,
                    fileType,
                    nowUtc,
                    cancellationToken);
            }

            return;
        }

        if (string.Equals(existingLink.LastContentHash, fileSnapshot.ContentHash, StringComparison.Ordinal))
        {
            existingLink.FilePath = Path.GetFullPath(filePath);
            existingLink.WatchedFolderPath = Path.GetFullPath(watchedFolderPath);
            existingLink.NormalizedWatchedFolderPath = normalizedWatchedFolderPath;
            existingLink.LastSeenAt = now;
            existingLink.DeletedAt = null;
            existingLink.UpdatedAt = nowUtc;

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (settings.StorageMode == WatchedFolderStorageModes.CopyToAppStorage)
        {
            await using FileStream contentStream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            StoredFileDto storedFile = await fileStorageService.SaveAsync(
                contentStream, existingLink.DocumentId, Path.GetFileName(filePath), cancellationToken);
            localPath = storedFile.LocalPath;
        }

        await UpdateDocumentForWatchedFileAsync(
            existingLink,
            filePath,
            localPath,
            watchedFolderPath,
            normalizedWatchedFolderPath,
            fileSnapshot,
            fileType,
            nowUtc,
            cancellationToken);
    }

    public async Task HandleDeletedAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        string normalizedFilePath;

        try
        {
            normalizedFilePath = NormalizePath(filePath);
        }
        catch (Exception)
        {
            return;
        }

        WatchedFileLink? link = await dbContext.WatchedFileLinks
            .FirstOrDefaultAsync(link => link.NormalizedFilePath == normalizedFilePath, cancellationToken);

        if (link is null)
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTime nowUtc = now.UtcDateTime;

        link.DeletedAt = now;
        link.LastSeenAt = now;
        link.UpdatedAt = nowUtc;

        Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(document => document.Id == link.DocumentId, cancellationToken);

        if (document is not null)
        {
            document.DeletedAt = now;
            document.Status = DocumentStatus.Deleted;
            document.SyncStatus = SyncStatus.DeletedLocal;
            document.UpdatedAt = nowUtc;

            dbContext.OperationLogs.Add(new OperationLog
            {
                CreatedAt = nowUtc,
                OperationType = "WatchedFolder.FileDeleted",
                EntityType = "Document",
                EntityId = document.Id.ToString(),
                Message = $"Marked watched document '{document.Name}' as deleted",
                MetadataJson = "{}"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleRenamedAsync(
        string oldFilePath,
        string newFilePath,
        string watchedFolderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldFilePath) || string.IsNullOrWhiteSpace(newFilePath) || string.IsNullOrWhiteSpace(watchedFolderPath))
        {
            return;
        }

        FileType newFileType = ResolveFileType(newFilePath);

        if (newFileType == FileType.Unknown)
        {
            await HandleDeletedAsync(oldFilePath, cancellationToken);
            return;
        }

        AppSettingsDto appSettings = await settingsService.GetAsync(cancellationToken);
        WatchedFoldersSettingsDto settings = appSettings.WatchedFolders ?? new WatchedFoldersSettingsDto(
            Enabled: false, DebounceMilliseconds: 1000, DeletePolicy: "MarkDeleted", Folders: []);

        WatchedFileFilterContext filterContext = filterService.CreateContext(settings);
        WatchedFileFilterResult filterResult = filterService.Evaluate(newFilePath, filterContext);

        if (!filterResult.IsAllowed)
        {
            await HandleDeletedAsync(oldFilePath, cancellationToken);
            return;
        }

        string normalizedOldFilePath;
        string normalizedNewFilePath;
        string normalizedWatchedFolderPath;

        try
        {
            normalizedOldFilePath = NormalizePath(oldFilePath);
            normalizedNewFilePath = NormalizePath(newFilePath);
            normalizedWatchedFolderPath = NormalizePath(watchedFolderPath);
        }
        catch (Exception)
        {
            return;
        }

        if (!IsFileInsideFolder(normalizedNewFilePath, normalizedWatchedFolderPath))
        {
            return;
        }

        WatchedFileLink? existingLink = await dbContext.WatchedFileLinks
            .FirstOrDefaultAsync(link => link.NormalizedFilePath == normalizedOldFilePath, cancellationToken);

        if (existingLink is null)
        {
            await HandleCreatedOrChangedAsync(newFilePath, watchedFolderPath, cancellationToken);
            return;
        }

        FileSnapshot? fileSnapshot = await TryReadFileSnapshotAsync(newFilePath, cancellationToken);

        if (fileSnapshot is null)
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTime nowUtc = now.UtcDateTime;

        if (string.Equals(existingLink.LastContentHash, fileSnapshot.ContentHash, StringComparison.Ordinal))
        {
            await UpdateRenamedMetadataAsync(
                existingLink,
                newFilePath,
                watchedFolderPath,
                normalizedNewFilePath,
                normalizedWatchedFolderPath,
                fileSnapshot,
                newFileType,
                now,
                nowUtc,
                cancellationToken);
            return;
        }

        string localPath = newFilePath;

        if (settings.StorageMode == WatchedFolderStorageModes.CopyToAppStorage)
        {
            await using FileStream contentStream = new FileStream(
                newFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            StoredFileDto storedFile = await fileStorageService.SaveAsync(
                contentStream, existingLink.DocumentId, Path.GetFileName(newFilePath), cancellationToken);
            localPath = storedFile.LocalPath;
        }

        await UpdateDocumentForWatchedFileAsync(
            existingLink,
            newFilePath,
            localPath,
            watchedFolderPath,
            normalizedWatchedFolderPath,
            fileSnapshot,
            newFileType,
            nowUtc,
            cancellationToken);
    }

    private async Task UpdateRenamedMetadataAsync(
        WatchedFileLink link,
        string newFilePath,
        string watchedFolderPath,
        string normalizedNewFilePath,
        string normalizedWatchedFolderPath,
        FileSnapshot fileSnapshot,
        FileType fileType,
        DateTimeOffset now,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string fullFilePath = Path.GetFullPath(newFilePath);

        link.FilePath = fullFilePath;
        link.NormalizedFilePath = normalizedNewFilePath;
        link.WatchedFolderPath = Path.GetFullPath(watchedFolderPath);
        link.NormalizedWatchedFolderPath = normalizedWatchedFolderPath;
        link.LastContentHash = fileSnapshot.ContentHash;
        link.LastSeenAt = now;
        link.DeletedAt = null;
        link.UpdatedAt = nowUtc;

        Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == link.DocumentId, cancellationToken);

        if (document is not null)
        {
            document.Name = Path.GetFileName(fullFilePath);
            document.DeletedAt = null;
            document.UpdatedAt = nowUtc;

            if (document.Status == DocumentStatus.Deleted)
            {
                document.Status = DocumentStatus.Queued;
            }
        }

        DocumentFile? documentFile = await dbContext.DocumentFiles
            .FirstOrDefaultAsync(f => f.DocumentId == link.DocumentId, cancellationToken);

        if (documentFile is not null)
        {
            documentFile.FileName = Path.GetFileName(fullFilePath);
            documentFile.LocalPath = fullFilePath;
            documentFile.FileType = fileType;
            documentFile.ContentHash = fileSnapshot.ContentHash;
            documentFile.SizeBytes = fileSnapshot.SizeBytes;
            documentFile.UpdatedAt = nowUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateDocumentForWatchedFileAsync(
        Guid documentId,
        string filePath,
        string localPath,
        string normalizedFilePath,
        string normalizedWatchedFolderPath,
        FileSnapshot fileSnapshot,
        FileType fileType,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string fullFilePath = Path.GetFullPath(filePath);

        Document document = new Document
        {
            Id = documentId,
            CreatedAt = nowUtc,
            Name = Path.GetFileName(fullFilePath),
            Status = DocumentStatus.Queued,
            SyncStatus = SyncStatus.LocalOnly
        };

        DocumentFile documentFile = new DocumentFile
        {
            CreatedAt = nowUtc,
            DocumentId = document.Id,
            FileName = Path.GetFileName(fullFilePath),
            FileType = fileType,
            LocalPath = localPath,
            ContentHash = fileSnapshot.ContentHash,
            SizeBytes = fileSnapshot.SizeBytes
        };

        WatchedFileLink link = new WatchedFileLink
        {
            CreatedAt = nowUtc,
            DocumentId = document.Id,
            WatchedFolderPath = Path.GetFullPath(Path.GetDirectoryName(filePath)!),
            NormalizedWatchedFolderPath = normalizedWatchedFolderPath,
            FilePath = fullFilePath,
            NormalizedFilePath = normalizedFilePath,
            LastContentHash = fileSnapshot.ContentHash,
            LastSeenAt = new DateTimeOffset(nowUtc, TimeSpan.Zero)
        };

        IngestionJob job = new IngestionJob
        {
            CreatedAt = nowUtc,
            DocumentId = document.Id,
            Status = IngestionJobStatus.Pending,
            CurrentStep = "Pending",
            ProgressPercent = 0
        };

        dbContext.Documents.Add(document);
        dbContext.DocumentFiles.Add(documentFile);
        dbContext.WatchedFileLinks.Add(link);
        dbContext.IngestionJobs.Add(job);

        dbContext.OperationLogs.Add(new OperationLog
        {
            CreatedAt = nowUtc,
            OperationType = "WatchedFolder.FileCreated",
            EntityType = "Document",
            EntityId = document.Id.ToString(),
            Message = $"Created ingestion job for watched file '{document.Name}'",
            MetadataJson = "{}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateDocumentForWatchedFileAsync(
        WatchedFileLink link,
        string filePath,
        string localPath,
        string watchedFolderPath,
        string normalizedWatchedFolderPath,
        FileSnapshot fileSnapshot,
        FileType fileType,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string fullFilePath = Path.GetFullPath(filePath);

        Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(document => document.Id == link.DocumentId, cancellationToken);

        if (document is null)
        {
            return;
        }

        DocumentFile? documentFile = await dbContext.DocumentFiles
            .FirstOrDefaultAsync(file => file.DocumentId == document.Id, cancellationToken);

        if (documentFile is null)
        {
            documentFile = new DocumentFile
            {
                CreatedAt = nowUtc,
                DocumentId = document.Id
            };

            dbContext.DocumentFiles.Add(documentFile);
        }

        DateTimeOffset nowOffset = new DateTimeOffset(nowUtc, TimeSpan.Zero);

        document.Name = Path.GetFileName(fullFilePath);
        document.Status = DocumentStatus.Queued;
        document.SyncStatus = SyncStatus.LocalOnly;
        document.DeletedAt = null;
        document.UpdatedAt = nowUtc;

        documentFile.FileName = Path.GetFileName(fullFilePath);
        documentFile.FileType = fileType;
        documentFile.LocalPath = localPath;
        documentFile.ContentHash = fileSnapshot.ContentHash;
        documentFile.SizeBytes = fileSnapshot.SizeBytes;
        documentFile.UpdatedAt = nowUtc;

        link.FilePath = fullFilePath;
        link.NormalizedFilePath = NormalizePath(fullFilePath);
        link.WatchedFolderPath = Path.GetFullPath(watchedFolderPath);
        link.NormalizedWatchedFolderPath = normalizedWatchedFolderPath;
        link.LastContentHash = fileSnapshot.ContentHash;
        link.LastSeenAt = nowOffset;
        link.DeletedAt = null;
        link.UpdatedAt = nowUtc;

        IngestionJob job = new IngestionJob
        {
            CreatedAt = nowUtc,
            DocumentId = document.Id,
            Status = IngestionJobStatus.Pending,
            CurrentStep = "Pending",
            ProgressPercent = 0
        };

        dbContext.IngestionJobs.Add(job);

        dbContext.OperationLogs.Add(new OperationLog
        {
            CreatedAt = nowUtc,
            OperationType = "WatchedFolder.FileChanged",
            EntityType = "Document",
            EntityId = document.Id.ToString(),
            Message = $"Created reindex job for watched file '{document.Name}'",
            MetadataJson = "{}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<FileSnapshot?> TryReadFileSnapshotAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= ReadRetryCount; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > int.MaxValue)
                {
                    return null;
                }

                await using FileStream stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                using SHA256 sha256 = SHA256.Create();
                byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);

                return new FileSnapshot(
                    ContentHash: Convert.ToHexString(hash).ToLowerInvariant(),
                    SizeBytes: Convert.ToInt32(fileInfo.Length));
            }
            catch (IOException) when (attempt < ReadRetryCount)
            {
                await Task.Delay(ReadRetryDelay, cancellationToken);
            }
            catch (UnauthorizedAccessException) when (attempt < ReadRetryCount)
            {
                await Task.Delay(ReadRetryDelay, cancellationToken);
            }
        }

        return null;
    }

    private static FileType ResolveFileType(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => FileType.Pdf,
            ".docx" => FileType.Docx,
            ".pptx" => FileType.Pptx,
            ".md" => FileType.Markdown,
            ".markdown" => FileType.Markdown,
            ".txt" => FileType.PlainText,
            ".html" => FileType.Html,
            ".htm" => FileType.Html,
            _ => FileType.Unknown
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

    private static bool IsFileInsideFolder(string normalizedFilePath, string normalizedFolderPath)
    {
        return normalizedFilePath.StartsWith(
            normalizedFolderPath + Path.DirectorySeparatorChar,
            PathComparison);
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private sealed record FileSnapshot(
        string ContentHash,
        int SizeBytes);
}
