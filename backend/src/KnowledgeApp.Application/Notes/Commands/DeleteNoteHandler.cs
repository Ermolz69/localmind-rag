using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class DeleteNoteHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Note? note = await dbContext.Notes
            .FirstOrDefaultAsync(x => x.Id == noteId && x.DeletedAt == null, cancellationToken);
        if (note is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Notes.NotFound, "Note was not found."));
        }

        note.DeletedAt = dateTimeProvider.UtcNow;
        note.SyncStatus = SyncStatus.DeletedLocal;
        note.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
