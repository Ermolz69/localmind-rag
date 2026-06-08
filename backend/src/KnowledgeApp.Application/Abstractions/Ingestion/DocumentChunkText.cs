namespace KnowledgeApp.Application.Abstractions.Ingestion;

public sealed record DocumentChunkText(
    string Text,
    string? CoreText,
    bool HasOverlap,
    string? HeadingPath,
    string? SectionTitle,
    string ChunkType,
    int? SourceStartOffset,
    int? SourceEndOffset,
    int TokenCount,
    string TokenizerId,
    string ChunkingAlgorithmId,
    string ChunkIdentityHash,
    string EmbeddingTextHash)
{
    public string EmbeddingText => Text;
}
