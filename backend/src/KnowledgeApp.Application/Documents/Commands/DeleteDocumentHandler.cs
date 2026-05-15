using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public sealed class DeleteDocumentHandler(IAppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.FindAsync([documentId], cancellationToken);
        if (document is null)
        {
            return false;
        }

        document.Status = DocumentStatus.Deleted;
        document.SyncStatus = SyncStatus.DeletedLocal;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
