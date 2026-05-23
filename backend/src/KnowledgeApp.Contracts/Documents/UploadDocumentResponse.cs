namespace KnowledgeApp.Contracts.Documents;

/// <summary>Response returned after a document upload is accepted.</summary>
/// <param name="DocumentId">Created local document identifier.</param>
/// <param name="IngestionJobId">Ingestion job created for the uploaded file.</param>
/// <param name="Status">Initial ingestion job status.</param>
public sealed record UploadDocumentResponse(Guid DocumentId, Guid IngestionJobId, string Status);

