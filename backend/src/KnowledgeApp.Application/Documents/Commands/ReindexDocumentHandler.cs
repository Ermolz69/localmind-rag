using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Documents;

public sealed class ReindexDocumentHandler(IAppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.FindAsync([documentId], cancellationToken);
        if (document is null)
        {
            return false;
        }

        dbContext.IngestionJobs.Add(new IngestionJob { DocumentId = documentId });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
