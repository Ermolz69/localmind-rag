namespace KnowledgeApp.Contracts.Documents;

public sealed record ReindexDocumentResponse(Guid DocumentId, Guid IngestionJobId, string Status);
