using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentsHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<DocumentDto>> HandleAsync(GetDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        var documents = dbContext.Documents.AsNoTracking();

        if (query.BucketId.HasValue)
        {
            documents = documents.Where(document => document.BucketId == query.BucketId.Value);
        }

        var documentRows = await documents
            .ToArrayAsync(cancellationToken);
        var documentIds = documentRows.Select(document => document.Id).ToArray();
        var failedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => documentIds.Contains(job.DocumentId) && job.LastError != null)
            .ToArrayAsync(cancellationToken);

        return documentRows
            .Select(document => new DocumentDto(
                document.Id,
                document.Name,
                document.Status.ToString(),
                document.CreatedAt,
                failedJobs
                    .Where(job => job.DocumentId == document.Id)
                    .OrderByDescending(job => job.ProcessedAt ?? job.CreatedAt)
                    .Select(job => job.LastError)
                    .FirstOrDefault()))
            .OrderByDescending(document => document.CreatedAt)
            .ToArray();
    }
}
