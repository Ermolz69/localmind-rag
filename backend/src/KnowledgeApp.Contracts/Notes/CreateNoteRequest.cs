namespace KnowledgeApp.Contracts.Notes;

public sealed record CreateNoteRequest(Guid? BucketId, string Title, string Markdown);
