namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Latest failed ingestion job diagnostic entry.</summary>
public sealed record DiagnosticsIngestionErrorDto(
    Guid JobId,
    Guid DocumentId,
    string DocumentName,
    string LastError,
    DateTimeOffset? ProcessedAt,
    int AttemptCount = 0,
    Guid? LastOperationId = null);
