using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public sealed record ChunkReuseMatch(
    ChunkCandidate Candidate,
    DocumentChunk ExistingChunk);
