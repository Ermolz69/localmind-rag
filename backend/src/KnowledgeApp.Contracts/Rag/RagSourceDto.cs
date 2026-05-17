namespace KnowledgeApp.Contracts.Rag;

public sealed record RagSourceDto(Guid DocumentId, string DocumentName, Guid ChunkId, int? PageNumber, double Score, string Snippet);


