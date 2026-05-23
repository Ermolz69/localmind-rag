namespace KnowledgeApp.Contracts.Buckets;

/// <summary>Bucket returned by the LocalMind API.</summary>
/// <param name="Id">Local bucket identifier.</param>
/// <param name="Name">Human-readable bucket name.</param>
/// <param name="Description">Optional bucket description.</param>
/// <param name="SyncStatus">Current synchronization status code.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest update, when available.</param>
public sealed record BucketDto(
    Guid Id,
    string Name,
    string? Description,
    int SyncStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
