namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsStorageDto(
    long DatabaseSizeBytes,
    long FilesSizeBytes,
    long IndexSizeBytes,
    long LogsSizeBytes);


