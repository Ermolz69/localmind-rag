using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Notes;

public sealed class CreateNoteHandler(IAppDbContext dbContext, NoteRequestValidator validator)
{
    public async Task<NoteDto> HandleAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Note note = new()
        {
            BucketId = request.BucketId,
            Title = request.Title.Trim(),
            Markdown = request.Markdown,
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoteMapper.ToDto(note);
    }
}
