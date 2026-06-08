using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public sealed class IncrementalChunkPlanner : IIncrementalChunkPlanner
{
    public ChunkDiffPlan BuildPlan(
        IReadOnlyList<ChunkCandidate> incomingChunks,
        IReadOnlyList<DocumentChunk> existingChunks)
    {
        ArgumentNullException.ThrowIfNull(incomingChunks);
        ArgumentNullException.ThrowIfNull(existingChunks);

        Dictionary<ChunkMatchKey, Queue<DocumentChunk>> existingChunksByKey = BuildExistingChunkLookup(existingChunks);
        HashSet<Guid> reusedExistingChunkIds = [];
        List<ChunkReuseMatch> reusedChunks = [];
        List<ChunkCandidate> newChunks = [];

        foreach (ChunkCandidate incomingChunk in incomingChunks)
        {
            if (string.IsNullOrWhiteSpace(incomingChunk.ChunkIdentityHash))
            {
                newChunks.Add(incomingChunk);
                continue;
            }

            ChunkMatchKey key = new ChunkMatchKey(incomingChunk.ChunkIdentityHash, incomingChunk.ChunkingAlgorithmId, incomingChunk.ChunkVersion);

            if (!existingChunksByKey.TryGetValue(key, out Queue<DocumentChunk>? matchingExistingChunks) ||
                matchingExistingChunks.Count == 0)
            {
                newChunks.Add(incomingChunk);
                continue;
            }

            DocumentChunk existingChunk = matchingExistingChunks.Dequeue();

            reusedExistingChunkIds.Add(existingChunk.Id);
            reusedChunks.Add(new ChunkReuseMatch(incomingChunk, existingChunk));
        }

        List<DocumentChunk> deletedChunks = existingChunks
            .Where(existingChunk => !reusedExistingChunkIds.Contains(existingChunk.Id))
            .OrderBy(existingChunk => existingChunk.Index)
            .ToList();

        return new ChunkDiffPlan(reusedChunks, newChunks, deletedChunks);
    }

    private static Dictionary<ChunkMatchKey, Queue<DocumentChunk>> BuildExistingChunkLookup(
        IReadOnlyList<DocumentChunk> existingChunks)
    {
        Dictionary<ChunkMatchKey, Queue<DocumentChunk>> existingChunksByKey = [];

        foreach (DocumentChunk existingChunk in existingChunks.OrderBy(chunk => chunk.Index))
        {
            if (string.IsNullOrWhiteSpace(existingChunk.ChunkIdentityHash))
            {
                continue;
            }

            ChunkMatchKey key = new ChunkMatchKey(existingChunk.ChunkIdentityHash, existingChunk.ChunkingAlgorithmId, existingChunk.ChunkVersion);

            if (!existingChunksByKey.TryGetValue(key, out Queue<DocumentChunk>? queue))
            {
                queue = new Queue<DocumentChunk>();
                existingChunksByKey[key] = queue;
            }

            queue.Enqueue(existingChunk);
        }

        return existingChunksByKey;
    }

    private readonly record struct ChunkMatchKey(string ChunkIdentityHash, string ChunkingAlgorithmId, int ChunkVersion);
}
