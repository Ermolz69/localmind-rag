using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentHandler(
    IAppDbContext dbContext,
    IFileStorageService fileStorage,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<UploadDocumentResponse> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var now = dateTimeProvider.UtcNow;
        var storedFile = await fileStorage.SaveAsync(command.Content, command.FileName.Trim(), cancellationToken);
        var document = new Document
        {
            BucketId = command.BucketId,
            CreatedAt = now,
            Name = storedFile.FileName,
            Status = DocumentStatus.Queued,
            SyncStatus = SyncStatus.LocalOnly,
        };
        var documentFile = new DocumentFile
        {
            ContentHash = storedFile.ContentHash,
            CreatedAt = now,
            DocumentId = document.Id,
            FileName = storedFile.FileName,
            FileType = ResolveFileType(storedFile.FileName, command.ContentType),
            LocalPath = storedFile.LocalPath,
            SizeBytes = storedFile.SizeBytes,
        };
        var ingestionJob = new IngestionJob
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

    private static void Validate(UploadDocumentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Content);

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ArgumentException("Document file name is required.", nameof(command));
        }

        if (command.Length <= 0)
        {
            throw new ArgumentException("Document file must not be empty.", nameof(command));
        }
    }

    private static FileType ResolveFileType(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => FileType.Pdf,
            ".docx" => FileType.Docx,
            ".pptx" => FileType.Pptx,
            ".md" or ".markdown" => FileType.Markdown,
            ".txt" => FileType.PlainText,
            ".html" or ".htm" => FileType.Html,
            _ when string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) => FileType.PlainText,
            _ => FileType.Unknown,
        };
    }
}
