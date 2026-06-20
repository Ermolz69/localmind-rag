namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Result of a log files cleanup operation.</summary>
/// <param name="DeletedFiles">Number of log files that were removed.</param>
/// <param name="FreedBytes">Total size of the removed files, in bytes.</param>
/// <param name="SkippedFiles">Number of log files that could not be removed (e.g. currently in use).</param>
public sealed record LogCleanupResultDto(
    int DeletedFiles,
    long FreedBytes,
    int SkippedFiles);
