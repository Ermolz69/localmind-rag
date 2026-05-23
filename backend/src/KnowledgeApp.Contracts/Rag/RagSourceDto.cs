namespace KnowledgeApp.Contracts.Rag;

/// <summary>Source chunk returned by semantic search or RAG chat.</summary>
/// <param name="DocumentId">Document that contains the source chunk.</param>
/// <param name="DocumentName">Display name of the source document.</param>
/// <param name="ChunkId">Local chunk identifier.</param>
/// <param name="PageNumber">Page number when the source came from paged content.</param>
/// <param name="Score">Similarity score assigned by vector search.</param>
/// <param name="Snippet">Short text excerpt from the source chunk.</param>
public sealed record RagSourceDto(Guid DocumentId, string DocumentName, Guid ChunkId, int? PageNumber, double Score, string Snippet);

