namespace KnowledgeApp.Contracts.Documents;

/// <summary>Response returned after a document reindex request is accepted.</summary>
/// <param name="DocumentId">Document queued for reindexing.</param>
/// <param name="IngestionJobId">Ingestion job created for the reindex operation.</param>
/// <param name="Status">Initial ingestion job status.</param>
public sealed record ReindexDocumentResponse(Guid DocumentId, Guid IngestionJobId, string Status);
