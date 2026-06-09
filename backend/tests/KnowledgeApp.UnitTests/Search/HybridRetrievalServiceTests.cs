using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.UnitTests.Search;

public sealed class HybridRetrievalServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Vector_Only_Result()
    {
        RagSourceDto vectorSource = Source("Vector.md", "Semantic result", 0.92);
        HybridRetrievalService service = new(
            new FakeVectorSearchService([vectorSource]),
            new FakeFullTextChunkSearchService([]));

        IReadOnlyList<HybridSearchResult> results = await service.SearchAsync(
            "semantic result",
            [1, 0],
            new HybridSearchOptions(Limit: 3));

        HybridSearchResult result = Assert.Single(results);
        Assert.Equal(vectorSource.ChunkId, result.ChunkId);
        Assert.True(result.HasVectorMatch);
        Assert.False(result.HasFullTextMatch);
        Assert.Equal(vectorSource.Score, result.VectorScore);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Keyword_Only_Result()
    {
        FullTextChunkSearchResult fullTextResult = FullTextSource("Security.md", "X-LocalMind-Token is required.", 1);
        HybridRetrievalService service = new(
            new FakeVectorSearchService([]),
            new FakeFullTextChunkSearchService([fullTextResult]));

        IReadOnlyList<HybridSearchResult> results = await service.SearchAsync(
            "X-LocalMind-Token",
            [1, 0],
            new HybridSearchOptions(Limit: 3));

        HybridSearchResult result = Assert.Single(results);
        Assert.Equal(fullTextResult.ChunkId, result.ChunkId);
        Assert.False(result.HasVectorMatch);
        Assert.True(result.HasFullTextMatch);
        Assert.Equal(fullTextResult.Rank, result.FullTextRank);
    }

    [Fact]
    public async Task SearchAsync_Should_Promote_Result_Found_By_Both_Channels()
    {
        Guid overlapChunkId = Guid.NewGuid();
        Guid overlapDocumentId = Guid.NewGuid();

        RagSourceDto vectorOnly = Source("VectorOnly.md", "Semantic only", 0.95);
        RagSourceDto overlapVector = Source("Overlap.md", "Semantic and keyword", 0.91, overlapDocumentId, overlapChunkId);
        FullTextChunkSearchResult overlapFullText = FullTextSource("Overlap.md", "Semantic and keyword", 1, overlapDocumentId, overlapChunkId);
        FullTextChunkSearchResult keywordOnly = FullTextSource("KeywordOnly.md", "Keyword only", 2);

        HybridRetrievalService service = new(
            new FakeVectorSearchService([vectorOnly, overlapVector]),
            new FakeFullTextChunkSearchService([overlapFullText, keywordOnly]));

        IReadOnlyList<HybridSearchResult> results = await service.SearchAsync(
            "keyword",
            [1, 0],
            new HybridSearchOptions(Limit: 3));

        Assert.Equal(overlapChunkId, results[0].ChunkId);
        Assert.True(results[0].HasBothMatches);
        Assert.Contains(results, result => result.ChunkId == vectorOnly.ChunkId);
        Assert.Contains(results, result => result.ChunkId == keywordOnly.ChunkId);
    }

    [Fact]
    public async Task SearchAsync_Should_Use_Expanded_Candidate_Limit_For_Each_Channel()
    {
        FakeVectorSearchService vectorSearch = new([]);
        FakeFullTextChunkSearchService fullTextSearch = new([]);
        HybridRetrievalService service = new(vectorSearch, fullTextSearch);

        await service.SearchAsync(
            "query",
            [1, 0],
            new HybridSearchOptions(Limit: 4));

        Assert.Equal(30, vectorSearch.Options?.Limit);
        Assert.Equal(30, fullTextSearch.Options?.Limit);
    }

    private static RagSourceDto Source(
        string documentName,
        string snippet,
        double score,
        Guid? documentId = null,
        Guid? chunkId = null)
    {
        return new RagSourceDto(
            documentId ?? Guid.NewGuid(),
            documentName,
            chunkId ?? Guid.NewGuid(),
            PageNumber: null,
            score,
            snippet);
    }

    private static FullTextChunkSearchResult FullTextSource(
        string documentName,
        string snippet,
        int rank,
        Guid? documentId = null,
        Guid? chunkId = null)
    {
        return new FullTextChunkSearchResult(
            documentId ?? Guid.NewGuid(),
            documentName,
            chunkId ?? Guid.NewGuid(),
            PageNumber: null,
            snippet,
            rank,
            Bm25Score: -1 * rank);
    }

    private sealed class FakeVectorSearchService(
        IReadOnlyList<RagSourceDto> results) : IVectorSearchService
    {
        public VectorSearchOptions? Options { get; private set; }

        public Task<IReadOnlyList<RagSourceDto>> SearchAsync(
            float[] queryVector,
            VectorSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(results);
        }
    }

    private sealed class FakeFullTextChunkSearchService(
        IReadOnlyList<FullTextChunkSearchResult> results) : IFullTextChunkSearchService
    {
        public FullTextSearchOptions? Options { get; private set; }

        public Task<IReadOnlyList<FullTextChunkSearchResult>> SearchAsync(
            string query,
            FullTextSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(results);
        }
    }
}
