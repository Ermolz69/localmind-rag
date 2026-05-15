using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class Bucket : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
    public Guid? LocalDeviceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
