namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Vector index health and counts.</summary>
public sealed record DiagnosticsVectorIndexDto(
    DiagnosticsHealthStatus Status,
    int DocumentChunksCount,
    int DocumentEmbeddingsCount);
