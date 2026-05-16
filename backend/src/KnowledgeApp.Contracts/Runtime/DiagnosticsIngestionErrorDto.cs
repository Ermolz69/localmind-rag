namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsIngestionErrorDto(
    Guid JobId,
    Guid DocumentId,
    string DocumentName,
    string LastError,
    DateTimeOffset? ProcessedAt);


