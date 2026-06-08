namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public sealed record ChunkCandidate(
    int Index,
    int? PageNumber,
    string Text,
    string ChunkIdentityHash,
    string EmbeddingTextHash,
    int ChunkVersion,
    string ChunkingAlgorithmId,
    string TokenizerId,
    int TokenCount,
    string ChunkType,
    string? HeadingPath = null,
    string? SectionTitle = null,
    int? SourceStartOffset = null,
    int? SourceEndOffset = null);
