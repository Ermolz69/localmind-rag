namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Storage size diagnostics in bytes.</summary>
public sealed record DiagnosticsStorageDto(
    long DatabaseSizeBytes,
    long FilesSizeBytes,
    long IndexSizeBytes,
    long LogsSizeBytes);

