using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class SyncOutboxItem : Entity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncOperation Operation { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public SyncStatus Status { get; set; } = SyncStatus.PendingUpload;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
