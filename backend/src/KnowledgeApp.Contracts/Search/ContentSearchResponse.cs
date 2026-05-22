namespace KnowledgeApp.Contracts.Search;

public sealed record ContentSearchResponse(
    IReadOnlyList<ContentSearchHitDto> Results);
