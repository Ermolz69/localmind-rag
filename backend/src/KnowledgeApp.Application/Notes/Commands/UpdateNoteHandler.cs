using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class UpdateNoteHandler(IAppDbContext dbContext, NoteRequestValidator validator)
{
    public async Task<UpdateNoteResult> HandleAsync(
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Domain.Entities.Note? note = await dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);
        if (note is null)
        {
            return new UpdateNoteResult(false);
        }

        note.Title = request.Title.Trim();
        note.Markdown = request.Markdown;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UpdateNoteResult(true);
    }
}
