using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Notes;

public sealed class CreateNoteHandler(IAppDbContext dbContext)
{
    public async Task<Note> HandleAsync(Note note, CancellationToken cancellationToken = default)
    {
        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return note;
    }
}
