namespace KnowledgeApp.Contracts.Ingestion;

/// <summary>Response returned after an ingestion job process request is accepted.</summary>
/// <param name="JobId">Ingestion job identifier.</param>
/// <param name="Status">Current ingestion job status.</param>
public sealed record ProcessIngestionJobResponse(Guid JobId, string Status);
