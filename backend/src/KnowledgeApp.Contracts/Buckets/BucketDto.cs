namespace KnowledgeApp.Contracts.Buckets;

public sealed record BucketDto(
    Guid Id,
    string Name,
    string? Description,
    int SyncStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
