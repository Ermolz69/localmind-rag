namespace KnowledgeApp.Contracts.Search;

public sealed record ContentSearchRequest(
    string Query,
    int Limit = 20,
    Guid? BucketId = null,
    Guid? DocumentId = null,
    Guid? NoteId = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    bool IncludeDocuments = true,
    bool IncludeNotes = true,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? FileType = null);
