using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class UpdateNoteHandler(
    INoteRepository noteRepository,
    IUnitOfWork unitOfWork,
    NoteRequestValidator validator)
{
    public async Task<Result> HandleAsync(
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Domain.Entities.Note? note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Notes.NotFound, "Note was not found."));
        }

        note.Title = request.Title.Trim();
        note.Markdown = request.Markdown;
        await noteRepository.UpdateAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
