using KnowledgeApp.Domain.Common;

namespace KnowledgeApp.Domain.Entities;

public sealed class SemanticCacheEntry : Entity
{
    public required string Question { get; init; }
    public required byte[] QuestionEmbedding { get; init; }
    public required int EmbeddingDimension { get; init; }
    public required string Answer { get; init; }
    public required string SourcesJson { get; init; }
}
