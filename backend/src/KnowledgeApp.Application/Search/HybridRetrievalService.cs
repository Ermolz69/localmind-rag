using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Search;

public sealed class HybridRetrievalService(
    IVectorSearchService vectorSearch,
    IFullTextChunkSearchService fullTextSearch) : IHybridRetrievalService
{
    private const int CandidateMultiplier = 4;
    private const int MinimumCandidateCount = 30;
    private const int ReciprocalRankFusionConstant = 60;

    public async Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        float[] queryVector,
        HybridSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options.Limit <= 0)
        {
            return [];
        }

        int candidateLimit = Math.Max(options.Limit * CandidateMultiplier, MinimumCandidateCount);

        Task<IReadOnlyList<RagSourceDto>> vectorTask = queryVector.Length == 0
            ? Task.FromResult<IReadOnlyList<RagSourceDto>>([])
            : vectorSearch.SearchAsync(
                queryVector,
                new VectorSearchOptions(candidateLimit, options.BucketId, options.DocumentId, options.Tags),
                cancellationToken);

        Task<IReadOnlyList<FullTextChunkSearchResult>> fullTextTask = fullTextSearch.SearchAsync(
            query,
            new FullTextSearchOptions(candidateLimit, options.BucketId, options.DocumentId, options.Tags),
            cancellationToken);

        await Task.WhenAll(vectorTask, fullTextTask);

        return Fuse(vectorTask.Result, fullTextTask.Result, options.Limit);
    }

    private static IReadOnlyList<HybridSearchResult> Fuse(
        IReadOnlyList<RagSourceDto> vectorResults,
        IReadOnlyList<FullTextChunkSearchResult> fullTextResults,
        int limit)
    {
        Dictionary<Guid, HybridAccumulator> byChunkId = [];

        for (int index = 0; index < vectorResults.Count; index++)
        {
            RagSourceDto source = vectorResults[index];
            int rank = index + 1;
            HybridAccumulator accumulator = GetOrCreate(byChunkId, source);

            accumulator.Score += ReciprocalRankScore(rank);
            accumulator.VectorScore = source.Score;
            accumulator.VectorRank = rank;
        }

        for (int index = 0; index < fullTextResults.Count; index++)
        {
            FullTextChunkSearchResult result = fullTextResults[index];
            int rank = result.Rank <= 0 ? index + 1 : result.Rank;
            HybridAccumulator accumulator = GetOrCreate(byChunkId, result);

            accumulator.Score += ReciprocalRankScore(rank);
            accumulator.FullTextScore = result.Bm25Score;
            accumulator.FullTextRank = rank;
            accumulator.Snippet = result.Snippet;
        }

        return byChunkId.Values
            .Select(accumulator => accumulator.ToResult())
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.HasBothMatches)
            .ThenBy(result => result.VectorRank ?? int.MaxValue)
            .ThenBy(result => result.FullTextRank ?? int.MaxValue)
            .ThenBy(result => result.DocumentName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.ChunkId)
            .Take(limit)
            .ToArray();
    }

    private static HybridAccumulator GetOrCreate(
        Dictionary<Guid, HybridAccumulator> byChunkId,
        RagSourceDto source)
    {
        if (byChunkId.TryGetValue(source.ChunkId, out HybridAccumulator? accumulator))
        {
            return accumulator;
        }

        accumulator = new HybridAccumulator(
            source.DocumentId,
            source.DocumentName,
            source.ChunkId,
            source.PageNumber,
            source.Snippet);

        byChunkId[source.ChunkId] = accumulator;

        return accumulator;
    }

    private static HybridAccumulator GetOrCreate(
        Dictionary<Guid, HybridAccumulator> byChunkId,
        FullTextChunkSearchResult result)
    {
        if (byChunkId.TryGetValue(result.ChunkId, out HybridAccumulator? accumulator))
        {
            return accumulator;
        }

        accumulator = new HybridAccumulator(
            result.DocumentId,
            result.DocumentName,
            result.ChunkId,
            result.PageNumber,
            result.Snippet);

        byChunkId[result.ChunkId] = accumulator;

        return accumulator;
    }

    private static double ReciprocalRankScore(int rank)
    {
        return 1d / (ReciprocalRankFusionConstant + rank);
    }

    private sealed class HybridAccumulator(
        Guid documentId,
        string documentName,
        Guid chunkId,
        int? pageNumber,
        string snippet)
    {
        public Guid DocumentId { get; } = documentId;

        public string DocumentName { get; } = documentName;

        public Guid ChunkId { get; } = chunkId;

        public int? PageNumber { get; } = pageNumber;

        public string Snippet { get; set; } = snippet;

        public double Score { get; set; }

        public double? VectorScore { get; set; }

        public int? VectorRank { get; set; }

        public double? FullTextScore { get; set; }

        public int? FullTextRank { get; set; }

        public HybridSearchResult ToResult()
        {
            return new HybridSearchResult(
                DocumentId,
                DocumentName,
                ChunkId,
                PageNumber,
                Snippet,
                Score,
                VectorScore,
                VectorRank,
                FullTextScore,
                FullTextRank);
        }
    }
}
