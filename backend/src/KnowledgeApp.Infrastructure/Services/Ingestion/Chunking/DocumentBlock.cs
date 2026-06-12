namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public sealed record DocumentBlock(
    DocumentBlockType Type,
    string Text,
    string? HeadingPath,
    string? SectionTitle,
    int? SourceStartOffset,
    int? SourceEndOffset,
    int TokenCount,
    bool IsAtomic);
