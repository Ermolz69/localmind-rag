namespace KnowledgeApp.Infrastructure.Services;

public sealed record EmbeddingModelManifest(
    string Id,
    string ModelName,
    string DisplayName,
    string Format,
    string Quantization,
    int Dimension,
    int ContextSize,
    string FileName,
    string SourceUrl,
    string SourceRepository,
    string SourceRevision,
    string Sha256,
    long SizeBytes,
    string License);
