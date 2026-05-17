namespace KnowledgeApp.Contracts.Notes;

public sealed record NoteDto(
    Guid Id,
    Guid? BucketId,
    string Title,
    string Markdown,
    int SyncStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
