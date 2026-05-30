namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Storage size diagnostics in bytes.</summary>
public sealed record DiagnosticsStorageDto(
    DiagnosticsHealthStatus Status,
    long DatabaseSizeBytes,
    long FilesSizeBytes,
    long IndexSizeBytes,
    long LogsSizeBytes);

