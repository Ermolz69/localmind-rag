namespace KnowledgeApp.Contracts.Ingestion;

/// <summary>Response returned after an ingestion job control action.</summary>
public sealed record IngestionJobActionResponse(Guid JobId, string Status, string Message);
