using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentHandler(
    IAppDbContext dbContext,
    IFileStorageService fileStorage,
    IDateTimeProvider dateTimeProvider,
    IBucketResolver bucketResolver,
    ILocalDeviceResolver localDeviceResolver,
    UploadDocumentCommandValidator validator,
    IAppDiagnosticLogger? diagnostics = null)
{
    public async Task<UploadDocumentResponse> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            "documents",
            "upload",
            new Dictionary<string, object?>
            {
                ["FileName"] = command.FileName,
                ["Length"] = command.Length,
                ["BucketId"] = command.BucketId,
            }) ?? Guid.Empty;

        validator.Validate(command);

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Bucket? bucket = await bucketResolver.ResolveForUploadAsync(command.BucketId, cancellationToken);
        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        Document? document = new Document
        {
            BucketId = bucket.Id,
            CreatedAt = now,
            LocalDeviceId = localDeviceId,
            Name = Path.GetFileName(command.FileName.Trim()),
            Status = DocumentStatus.Queued,
            SyncStatus = SyncStatus.LocalOnly,
        };
        StoredFileDto? storedFile = await fileStorage.SaveAsync(command.Content, document.Id, command.FileName.Trim(), cancellationToken);
        DocumentFile? documentFile = new DocumentFile
        {
            ContentHash = storedFile.ContentHash,
            CreatedAt = now,
            DocumentId = document.Id,
            FileName = storedFile.FileName,
            FileType = DocumentFileTypeResolver.Resolve(storedFile.FileName),
            LocalPath = storedFile.LocalPath,
            SizeBytes = storedFile.SizeBytes,
        };
        IngestionJob? ingestionJob = new IngestionJob
        {
            CreatedAt = now,
            DocumentId = document.Id,
            Status = IngestionJobStatus.Queued,
        };

        diagnostics?.LogStep(
            operationId,
            "document-created",
            new Dictionary<string, object?>
            {
                ["DocumentId"] = document.Id,
                ["BucketId"] = bucket.Id,
                ["IngestionJobId"] = ingestionJob.Id,
            });

        dbContext.Documents.Add(document);
        dbContext.DocumentFiles.Add(documentFile);
        dbContext.IngestionJobs.Add(ingestionJob);

        await dbContext.SaveChangesAsync(cancellationToken);

        diagnostics?.LogStep(
            operationId,
            "upload-saved",
            new Dictionary<string, object?>
            {
                ["DocumentId"] = document.Id,
                ["IngestionJobId"] = ingestionJob.Id,
                ["Status"] = document.Status.ToString(),
            });

        return new UploadDocumentResponse(document.Id, ingestionJob.Id, document.Status.ToString());
    }
}
