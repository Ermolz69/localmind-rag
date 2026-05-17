using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class Document : Entity
{
    public Guid? BucketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
    public Guid? LocalDeviceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
