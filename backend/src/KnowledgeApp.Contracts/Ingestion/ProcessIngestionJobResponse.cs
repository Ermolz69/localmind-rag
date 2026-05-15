namespace KnowledgeApp.Contracts.Ingestion;

public sealed record ProcessIngestionJobResponse(Guid JobId, string Status);
