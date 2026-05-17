namespace KnowledgeApp.Contracts.Documents;

public sealed record UploadDocumentResponse(Guid DocumentId, Guid IngestionJobId, string Status);


