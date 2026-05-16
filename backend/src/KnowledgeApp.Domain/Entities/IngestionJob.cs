using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class IngestionJob : Entity
{
    public Guid DocumentId { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Queued;
    public string? LastError { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
