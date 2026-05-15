using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class UpdateNoteHandler(IAppDbContext dbContext)
{
    public async Task<UpdateNoteResult> HandleAsync(
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var note = await dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);
        if (note is null)
        {
            return new UpdateNoteResult(false);
        }

        note.Title = request.Title;
        note.Markdown = request.Markdown;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UpdateNoteResult(true);
    }
}
