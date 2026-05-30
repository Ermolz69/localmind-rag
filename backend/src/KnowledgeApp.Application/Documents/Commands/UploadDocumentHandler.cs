using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    IDateTimeProvider dateTimeProvider,
    IBucketResolver bucketResolver,
    ILocalDeviceResolver localDeviceResolver,
    IDomainEventPublisher eventPublisher,
    UploadDocumentCommandValidator validator,
    IAppDiagnosticLogger? diagnostics = null)
{
    public async Task<Result<UploadDocumentResponse>> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Documents,
            DiagnosticNames.Operations.DocumentUpload,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.FileName] = Path.GetFileName(command.FileName),
                [DiagnosticNames.Properties.Length] = command.Length,
                [DiagnosticNames.Properties.BucketId] = command.BucketId,
            }) ?? Guid.Empty;

        Result validation = validator.Validate(command);
        if (!validation.IsSuccess)
        {
            return Result<UploadDocumentResponse>.Failure(validation);
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Result<Bucket> bucketResult = await bucketResolver.ResolveForUploadAsync(command.BucketId, cancellationToken);
        if (!bucketResult.IsSuccess)
        {
            return Result<UploadDocumentResponse>.Failure(bucketResult.Error!);
        }

        Bucket bucket = bucketResult.Value!;
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
        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.DocumentCreated,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.DocumentId] = document.Id,
                [DiagnosticNames.Properties.BucketId] = bucket.Id,
            });

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.AddFileAsync(documentFile, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await eventPublisher.PublishAsync(new DocumentUploadedEvent(document.Id, now), cancellationToken);

        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.UploadSaved,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.DocumentId] = document.Id,
                [DiagnosticNames.Properties.Status] = document.Status.ToString(),
            });

        return Result<UploadDocumentResponse>.Success(new UploadDocumentResponse(document.Id, null, document.Status.ToString()));
    }
}
