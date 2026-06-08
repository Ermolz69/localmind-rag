using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Domain.Entities;
using Xunit;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IncrementalChunkPlannerTests
{
    [Fact]
    public void BuildPlan_Should_ReuseAllChunks_WhenIncomingChunksAreUnchanged()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0),
            CreateExistingChunk("hash-b", 1),
            CreateExistingChunk("hash-c", 2)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0),
            CreateCandidate("hash-b", 1),
            CreateCandidate("hash-c", 2)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Equal(3, plan.ReusedChunks.Count);
        Assert.Empty(plan.NewChunks);
        Assert.Empty(plan.DeletedChunks);

        Assert.Same(existingChunks[0], plan.ReusedChunks[0].ExistingChunk);
        Assert.Same(existingChunks[1], plan.ReusedChunks[1].ExistingChunk);
        Assert.Same(existingChunks[2], plan.ReusedChunks[2].ExistingChunk);
    }

    [Fact]
    public void BuildPlan_Should_CreateNewChunkAndDeleteOldChunk_WhenChunkTextChanged()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0),
            CreateExistingChunk("hash-b", 1),
            CreateExistingChunk("hash-c", 2)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0),
            CreateCandidate("hash-b-modified", 1),
            CreateCandidate("hash-c", 2)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Equal(2, plan.ReusedChunks.Count);
        Assert.Single(plan.NewChunks);
        Assert.Single(plan.DeletedChunks);

        Assert.Equal("hash-b-modified", plan.NewChunks[0].ChunkIdentityHash);
        Assert.Same(existingChunks[1], plan.DeletedChunks[0]);
    }

    [Fact]
    public void BuildPlan_Should_DeleteMissingChunk_WhenChunkWasRemoved()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0),
            CreateExistingChunk("hash-b", 1),
            CreateExistingChunk("hash-c", 2)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0),
            CreateCandidate("hash-c", 1)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Equal(2, plan.ReusedChunks.Count);
        Assert.Empty(plan.NewChunks);
        Assert.Single(plan.DeletedChunks);

        Assert.Same(existingChunks[1], plan.DeletedChunks[0]);
    }

    [Fact]
    public void BuildPlan_Should_ReuseMovedChunks_WhenIdentityHashesAreSame()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0),
            CreateExistingChunk("hash-b", 1),
            CreateExistingChunk("hash-c", 2)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-c", 0),
            CreateCandidate("hash-a", 1),
            CreateCandidate("hash-b", 2)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Equal(3, plan.ReusedChunks.Count);
        Assert.Empty(plan.NewChunks);
        Assert.Empty(plan.DeletedChunks);

        Assert.Same(existingChunks[2], plan.ReusedChunks[0].ExistingChunk);
        Assert.Same(existingChunks[0], plan.ReusedChunks[1].ExistingChunk);
        Assert.Same(existingChunks[1], plan.ReusedChunks[2].ExistingChunk);
    }

    [Fact]
    public void BuildPlan_Should_DeleteOnlyUnmatchedOccurrence_WhenDuplicateChunksExist()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0),
            CreateExistingChunk("hash-a", 1),
            CreateExistingChunk("hash-b", 2)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0),
            CreateCandidate("hash-b", 1)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Equal(2, plan.ReusedChunks.Count);
        Assert.Empty(plan.NewChunks);
        Assert.Single(plan.DeletedChunks);

        Assert.Same(existingChunks[1], plan.DeletedChunks[0]);
    }

    [Fact]
    public void BuildPlan_Should_NotReuseChunk_WhenChunkVersionChanged()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk("hash-a", 0, chunkVersion: 1)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0, chunkVersion: 2)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Empty(plan.ReusedChunks);
        Assert.Single(plan.NewChunks);
        Assert.Single(plan.DeletedChunks);

        Assert.Same(existingChunks[0], plan.DeletedChunks[0]);
    }

    [Fact]
    public void BuildPlan_Should_NotReuseExistingChunk_WhenExistingChunkHasEmptyHash()
    {
        IncrementalChunkPlanner planner = new IncrementalChunkPlanner();

        List<DocumentChunk> existingChunks =
        [
            CreateExistingChunk(string.Empty, 0)
        ];

        List<ChunkCandidate> incomingChunks =
        [
            CreateCandidate("hash-a", 0)
        ];

        ChunkDiffPlan plan = planner.BuildPlan(incomingChunks, existingChunks);

        Assert.Empty(plan.ReusedChunks);
        Assert.Single(plan.NewChunks);
        Assert.Single(plan.DeletedChunks);

        Assert.Same(existingChunks[0], plan.DeletedChunks[0]);
    }

    private static DocumentChunk CreateExistingChunk(
        string textHash,
        int index,
        int chunkVersion = IndexingVersions.CurrentChunkVersion)
    {
        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Index = index,
            PageNumber = null,
            Text = $"Existing text for {textHash}",
            ChunkIdentityHash = textHash,
            EmbeddingTextHash = textHash,
            ChunkingAlgorithmId = "test-alg",
            TokenizerId = "test-tokenizer",
            ChunkType = "unknown",
            ChunkVersion = chunkVersion
        };
    }

    private static ChunkCandidate CreateCandidate(
        string textHash,
        int index,
        int chunkVersion = IndexingVersions.CurrentChunkVersion)
    {
        return new ChunkCandidate(
            Index: index,
            PageNumber: null,
            Text: $"Incoming text for {textHash}",
            ChunkIdentityHash: textHash,
            EmbeddingTextHash: textHash,
            ChunkVersion: chunkVersion,
            ChunkingAlgorithmId: "test-alg",
            TokenizerId: "test-tokenizer",
            TokenCount: 10,
            ChunkType: "unknown");
    }
}
