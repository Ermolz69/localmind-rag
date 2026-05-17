using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class DeleteDocumentHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<DeleteDocumentResult> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(x => x.Id == documentId && x.DeletedAt == null, cancellationToken);
        if (document is null)
        {
            return new DeleteDocumentResult(false);
        }

        document.DeletedAt = dateTimeProvider.UtcNow;
        document.Status = DocumentStatus.Deleted;
        document.SyncStatus = SyncStatus.DeletedLocal;
        document.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteDocumentResult(true);
    }
}
