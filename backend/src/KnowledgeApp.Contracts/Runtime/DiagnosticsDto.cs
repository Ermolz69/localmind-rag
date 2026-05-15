namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsPathsDto(
    string DatabasePath,
    string FilesPath,
    string IndexPath,
    string LogsPath);

public sealed record DiagnosticsStorageDto(
    long DatabaseSizeBytes,
    long FilesSizeBytes,
    long IndexSizeBytes,
    long LogsSizeBytes);

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

public sealed record DiagnosticsIngestionErrorDto(
    Guid JobId,
    Guid DocumentId,
    string DocumentName,
    string LastError,
    DateTimeOffset? ProcessedAt);

public sealed record DiagnosticsRuntimeDto(
    string RuntimeMode,
    string LocalApiVersion,
    RuntimeStatusDto AiRuntimeStatus);

public sealed record DiagnosticsDto(
    DiagnosticsPathsDto Paths,
    DiagnosticsStorageDto Storage,
    DiagnosticsCountsDto Counts,
    IReadOnlyList<DiagnosticsIngestionErrorDto> LatestErrors,
    DiagnosticsRuntimeDto Runtime);
