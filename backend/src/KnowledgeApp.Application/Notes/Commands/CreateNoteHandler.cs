using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Notes;

public sealed class CreateNoteHandler(IAppDbContext dbContext)
{
    public async Task<NoteDto> HandleAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var note = new Note
        {
            BucketId = request.BucketId,
            Title = request.Title,
            Markdown = request.Markdown,
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoteMapper.ToDto(note);
    }
}
