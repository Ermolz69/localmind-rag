using KnowledgeApp.Domain.Common;

namespace KnowledgeApp.Domain.Entities;

public sealed class DocumentChunkTag : Entity
{
    public Guid DocumentChunkId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
