namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Latest failed ingestion job diagnostic entry.</summary>
public sealed record DiagnosticsIngestionErrorDto(
    Guid JobId,
    Guid DocumentId,
    string DocumentName,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset? ProcessedAt,
    int RetryCount = 0,
    Guid? LastOperationId = null);
