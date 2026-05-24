using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class DeleteDocumentHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(x => x.Id == documentId && x.DeletedAt == null, cancellationToken);
        if (document is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        document.DeletedAt = dateTimeProvider.UtcNow;
        document.Status = DocumentStatus.Deleted;
        document.SyncStatus = SyncStatus.DeletedLocal;
        document.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
