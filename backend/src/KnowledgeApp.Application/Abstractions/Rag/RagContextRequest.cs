namespace KnowledgeApp.Application.Abstractions;

public sealed record RagContextRequest(
    Guid ConversationId,
    string Question,
    int Limit = 12,
    Guid? BucketId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? FileType = null,
    IReadOnlyDictionary<string, string>? Tags = null);
