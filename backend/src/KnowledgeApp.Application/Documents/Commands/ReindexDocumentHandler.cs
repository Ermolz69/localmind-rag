using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class ReindexDocumentHandler(IAppDbContext dbContext)
{
    public async Task<Result<ReindexDocumentResponse>> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(x => x.Id == documentId && x.DeletedAt == null, cancellationToken);
        if (document is null)
        {
            return Result<ReindexDocumentResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        IngestionJob? job = new IngestionJob { DocumentId = documentId };
        dbContext.IngestionJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ReindexDocumentResponse>.Success(new ReindexDocumentResponse(documentId, job.Id, job.Status.ToString()));
    }
}
