using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public sealed class ChunkDiffPlan
{
    public ChunkDiffPlan(
        IReadOnlyList<ChunkReuseMatch> reusedChunks,
        IReadOnlyList<ChunkCandidate> newChunks,
        IReadOnlyList<DocumentChunk> deletedChunks)
    {
        ReusedChunks = reusedChunks;
        NewChunks = newChunks;
        DeletedChunks = deletedChunks;
    }

    public IReadOnlyList<ChunkReuseMatch> ReusedChunks { get; }

    public IReadOnlyList<ChunkCandidate> NewChunks { get; }

    public IReadOnlyList<DocumentChunk> DeletedChunks { get; }
}
