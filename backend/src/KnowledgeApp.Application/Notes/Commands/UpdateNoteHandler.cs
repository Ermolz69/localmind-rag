using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Notes;

public sealed class UpdateNoteHandler(IAppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid noteId, Note request, CancellationToken cancellationToken = default)
    {
        var note = await dbContext.Notes.FindAsync([noteId], cancellationToken);
        if (note is null)
        {
            return false;
        }

        note.Title = request.Title;
        note.Markdown = request.Markdown;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
