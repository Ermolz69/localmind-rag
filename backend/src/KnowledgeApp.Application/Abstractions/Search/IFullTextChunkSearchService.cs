namespace KnowledgeApp.Application.Abstractions;

public sealed record FullTextSearchOptions(
    int Limit = 8,
    Guid? BucketId = null,
    Guid? DocumentId = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? FileType = null);

public sealed record FullTextChunkSearchResult(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int? PageNumber,
    string Snippet,
    int Rank,
    double Bm25Score);

public interface IFullTextChunkSearchService
{
    Task<IReadOnlyList<FullTextChunkSearchResult>> SearchAsync(
        string query,
        FullTextSearchOptions options,
        CancellationToken cancellationToken = default);
}

public interface IFullTextChunkIndex
{
    Task SyncDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
