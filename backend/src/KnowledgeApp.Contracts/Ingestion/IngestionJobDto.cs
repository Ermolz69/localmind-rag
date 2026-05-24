namespace KnowledgeApp.Contracts.Ingestion;

/// <summary>Current state and retry/cancel affordances for an ingestion job.</summary>
public sealed record IngestionJobDto(
    Guid Id,
    Guid DocumentId,
    string Status,
    int ProgressPercent,
    string CurrentStep,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ProcessedAt,
    string? ErrorCode,
    string? ErrorMessage,
    int RetryCount,
    bool CanRetry,
    bool CanCancel,
    Guid? LastOperationId);
