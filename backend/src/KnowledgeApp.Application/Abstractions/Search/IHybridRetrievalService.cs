using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public sealed record HybridSearchOptions(
    int Limit = 8,
    Guid? BucketId = null,
    Guid? DocumentId = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? FileType = null);

public sealed record HybridSearchResult(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int? PageNumber,
    string Snippet,
    double Score,
    double? VectorScore,
    int? VectorRank,
    double? FullTextScore,
    int? FullTextRank)
{
    public bool HasVectorMatch => VectorRank.HasValue;

    public bool HasFullTextMatch => FullTextRank.HasValue;

    public bool HasBothMatches => HasVectorMatch && HasFullTextMatch;

    public RagSourceDto ToRagSource()
    {
        return new RagSourceDto(
            DocumentId,
            DocumentName,
            ChunkId,
            PageNumber,
            Score,
            Snippet);
    }
}

public interface IHybridRetrievalService
{
    Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        float[] queryVector,
        HybridSearchOptions options,
        CancellationToken cancellationToken = default);
}
