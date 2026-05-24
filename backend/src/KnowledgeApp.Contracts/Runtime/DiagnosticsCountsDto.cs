namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Local database counts used by diagnostics.</summary>
public sealed record DiagnosticsCountsDto(
    int BucketsCount,
    int DocumentsCount,
    int DocumentFilesCount,
    int DocumentChunksCount,
    int DocumentEmbeddingsCount,
    int NotesCount,
    int ConversationsCount,
    int PendingIngestionJobsCount,
    int FailedIngestionJobsCount,
    int RunningIngestionJobsCount = 0,
    int CancelledIngestionJobsCount = 0,
    Guid? LastProcessedIngestionJobId = null);
