using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class Note : Entity
{
    public Guid? BucketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
    public Guid? LocalDeviceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
