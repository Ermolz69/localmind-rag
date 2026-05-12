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

        var result = await documents
            .Select(document => new DocumentDto(document.Id, document.Name, document.Status.ToString(), document.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return result
            .OrderByDescending(document => document.CreatedAt)
            .ToArray();
    }
}
