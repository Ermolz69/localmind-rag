namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Database health and counts.</summary>
public sealed record DiagnosticsDatabaseDto(
    DiagnosticsHealthStatus Status,
    int BucketsCount,
    int DocumentsCount,
    int DocumentFilesCount,
    int NotesCount,
    int ConversationsCount,
    int PendingIngestionJobsCount,
    int RunningIngestionJobsCount,
    int FailedIngestionJobsCount,
    int CancelledIngestionJobsCount,
    Guid? LastProcessedIngestionJobId);
