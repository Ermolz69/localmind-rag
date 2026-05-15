using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class ReindexDocumentHandler(IAppDbContext dbContext)
{
    public async Task<ReindexDocumentResult> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        if (document is null)
        {
            return new ReindexDocumentResult(false, null);
        }

        var job = new IngestionJob { DocumentId = documentId };
        dbContext.IngestionJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ReindexDocumentResult(true, job.Id);
    }
}
