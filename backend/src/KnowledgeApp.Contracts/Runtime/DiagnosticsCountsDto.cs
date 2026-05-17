namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsCountsDto(
    int BucketsCount,
    int DocumentsCount,
    int DocumentFilesCount,
    int DocumentChunksCount,
    int DocumentEmbeddingsCount,
    int NotesCount,
    int ConversationsCount,
    int PendingIngestionJobsCount,
    int FailedIngestionJobsCount);


