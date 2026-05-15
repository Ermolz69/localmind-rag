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
    UploadDocumentCommandValidator validator)
{
    public async Task<UploadDocumentResponse> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
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

        dbContext.Documents.Add(document);
        dbContext.DocumentFiles.Add(documentFile);
        dbContext.IngestionJobs.Add(ingestionJob);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UploadDocumentResponse(document.Id, ingestionJob.Id, document.Status.ToString());
    }
}
