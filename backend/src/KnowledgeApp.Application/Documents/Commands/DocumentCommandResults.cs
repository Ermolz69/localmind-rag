namespace KnowledgeApp.Application.Documents;

public sealed record DeleteDocumentResult(bool Found);

public sealed record ReindexDocumentResult(bool Found, Guid? JobId);
