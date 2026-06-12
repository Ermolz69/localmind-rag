namespace KnowledgeApp.Contracts.Search;

public sealed record RetrievalFilters(
    Guid? BucketId = null,
    Guid? DocumentId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? FileType = null,
    IReadOnlyDictionary<string, string>? Tags = null);
