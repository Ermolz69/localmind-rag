namespace KnowledgeApp.Contracts.Search;

public sealed record ContentSearchHitDto(
    string SourceType,
    Guid SourceId,
    Guid? ChunkId,
    string Title,
    int? PageNumber,
    double Score,
    string Snippet);
