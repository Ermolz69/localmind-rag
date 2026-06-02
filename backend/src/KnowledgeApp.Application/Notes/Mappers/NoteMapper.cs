using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Notes;

public static class NoteMapper
{
    public static NoteDto ToDto(Note note)
    {
        var tags = note.Tags?.Count > 0
            ? (IReadOnlyDictionary<string, string>)note.Tags.ToDictionary(t => t.Key, t => t.Value)
            : null;

        return new NoteDto(
            note.Id,
            note.BucketId,
            note.Title,
            note.Markdown,
            (int)note.SyncStatus,
            note.CreatedAt,
            note.UpdatedAt,
            tags);
    }
}
