using KnowledgeApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class DeleteNoteHandler(IAppDbContext dbContext)
{
    public async Task<DeleteNoteResult> HandleAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var note = await dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);
        if (note is null)
        {
            return new DeleteNoteResult(false);
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteNoteResult(true);
    }
}
