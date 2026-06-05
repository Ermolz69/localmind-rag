using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public interface IIncrementalChunkPlanner
{
    ChunkDiffPlan BuildPlan(
        IReadOnlyList<ChunkCandidate> incomingChunks,
        IReadOnlyList<DocumentChunk> existingChunks);
}
