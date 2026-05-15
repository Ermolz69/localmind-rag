using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Application.Notes;

public sealed class DeleteNoteHandler(IAppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var note = await dbContext.Notes.FindAsync([noteId], cancellationToken);
        if (note is null)
        {
            return false;
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
