using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class DocumentChunk : Entity
{
    public Guid DocumentId { get; set; }

    public int Index { get; set; }

    public int? PageNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? HeadingPath { get; set; }

    public string? SectionTitle { get; set; }

    public string ChunkType { get; set; } = "unknown";

    public int? SourceStartOffset { get; set; }

    public int? SourceEndOffset { get; set; }

    public int TokenCount { get; set; }

    public string TokenizerId { get; set; } = string.Empty;

    public string ChunkingAlgorithmId { get; set; } = string.Empty;

    public string ChunkIdentityHash { get; set; } = string.Empty;

    public string EmbeddingTextHash { get; set; } = string.Empty;

    public int ChunkVersion { get; set; }
}
