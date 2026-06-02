namespace KnowledgeApp.Infrastructure.Services;

public sealed record ChatModelManifest(
    string Id,
    string ModelName,
    string DisplayName,
    string Format,
    string Quantization,
    int ContextSize,
    string FileName,
    string SourceUrl,
    string SourceRepository,
    string SourceRevision,
    string BaseModel,
    string Sha256,
    long SizeBytes,
    string License);
