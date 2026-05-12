using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentByIdHandler(IAppDbContext dbContext)
{
    public async Task<DocumentDto?> HandleAsync(GetDocumentByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == query.DocumentId)
            .Select(document => new DocumentDto(document.Id, document.Name, document.Status.ToString(), document.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
