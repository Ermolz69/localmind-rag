using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class DeleteDocumentHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        document.DeletedAt = dateTimeProvider.UtcNow;
        document.Status = DocumentStatus.Deleted;
        document.SyncStatus = SyncStatus.DeletedLocal;
        document.UpdatedAt = dateTimeProvider.UtcNow;

        await documentRepository.UpdateAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
