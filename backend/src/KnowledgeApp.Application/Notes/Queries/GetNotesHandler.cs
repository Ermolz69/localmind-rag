using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class GetNotesHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<Note>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Notes.AsNoTracking().ToArrayAsync(cancellationToken);
    }
}
