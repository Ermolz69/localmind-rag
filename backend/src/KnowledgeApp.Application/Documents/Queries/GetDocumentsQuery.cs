namespace KnowledgeApp.Application.Documents;

public sealed record GetDocumentsQuery(Guid? BucketId = null, string? Status = null, string? Cursor = null, int Limit = 50);
