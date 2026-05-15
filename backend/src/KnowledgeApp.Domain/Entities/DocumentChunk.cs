using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class DocumentChunk : Entity
{
    public Guid DocumentId { get; set; }
    public int Index { get; set; }
    public int? PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}
