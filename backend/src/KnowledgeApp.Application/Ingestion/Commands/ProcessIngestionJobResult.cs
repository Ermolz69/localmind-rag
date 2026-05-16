namespace KnowledgeApp.Application.Ingestion;

public sealed record ProcessIngestionJobResult(bool Found, Guid? JobId, string? Status);
