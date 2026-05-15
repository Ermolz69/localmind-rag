using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class DeleteDocumentHandler(IAppDbContext dbContext)
{
    public async Task<DeleteDocumentResult> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        if (document is null)
        {
            return new DeleteDocumentResult(false);
        }

        document.Status = DocumentStatus.Deleted;
        document.SyncStatus = SyncStatus.DeletedLocal;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteDocumentResult(true);
    }
}
