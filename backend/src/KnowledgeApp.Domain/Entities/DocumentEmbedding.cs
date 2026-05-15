using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class DocumentEmbedding : Entity
{
    public Guid DocumentChunkId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public byte[] Embedding { get; set; } = [];
}
