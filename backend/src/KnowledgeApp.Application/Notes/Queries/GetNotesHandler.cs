using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class GetNotesHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<NoteDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var notes = await dbContext.Notes
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return notes.Select(NoteMapper.ToDto).ToArray();
    }
}
