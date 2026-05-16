using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class DeleteNoteHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<DeleteNoteResult> HandleAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Note? note = await dbContext.Notes
            .FirstOrDefaultAsync(x => x.Id == noteId && x.DeletedAt == null, cancellationToken);
        if (note is null)
        {
            return new DeleteNoteResult(false);
        }

        note.DeletedAt = dateTimeProvider.UtcNow;
        note.SyncStatus = SyncStatus.DeletedLocal;
        note.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteNoteResult(true);
    }
}
