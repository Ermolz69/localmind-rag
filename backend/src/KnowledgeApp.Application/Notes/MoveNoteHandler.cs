using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class MoveNoteHandler(
    INoteRepository noteRepository,
    IUnitOfWork unitOfWork,
    INoteFolderService noteFolderService)
{
    public async Task<Result<NoteDto>> HandleAsync(
        Guid noteId,
        MoveNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null)
            return Result<NoteDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Notes.NotFound, "Note was not found."));

        if (request.FolderId.HasValue)
        {
            Result parentValidation = await noteFolderService.ValidateParentAsync(request.BucketId, request.FolderId, cancellationToken);
            if (!parentValidation.IsSuccess)
                return Result<NoteDto>.Failure(parentValidation);
        }

        note.BucketId = request.BucketId;
        note.FolderId = request.FolderId;

        await noteRepository.UpdateAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new NoteDto(
            note.Id,
            note.BucketId,
            note.FolderId,
            note.Title,
            note.Markdown,
            (int)note.SyncStatus,
            note.CreatedAt,
            note.UpdatedAt,
            note.Tags.ToDictionary(t => t.Key, t => t.Value));

        return Result<NoteDto>.Success(dto);
    }
}
