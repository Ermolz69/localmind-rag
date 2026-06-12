namespace KnowledgeApp.Application.Notes;

public sealed record GetNotesQuery(Guid? BucketId = null, Guid? FolderId = null, string? Query = null, string? Cursor = null, int Limit = 50);
