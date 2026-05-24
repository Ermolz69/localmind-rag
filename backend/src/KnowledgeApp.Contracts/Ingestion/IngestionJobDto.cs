namespace KnowledgeApp.Contracts.Ingestion;

/// <summary>Current state and retry/cancel affordances for an ingestion job.</summary>
public sealed record IngestionJobDto(
    Guid Id,
    Guid DocumentId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ProcessedAt,
    string? LastError,
    int AttemptCount,
    bool CanRetry,
    bool CanCancel,
    Guid? LastOperationId);
