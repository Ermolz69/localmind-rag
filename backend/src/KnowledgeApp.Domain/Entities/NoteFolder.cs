using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class NoteFolder : Entity
{
    public Guid BucketId { get; set; }
    public Guid? ParentFolderId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Guid? LocalDeviceId { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
    public DateTimeOffset? DeletedAt { get; set; }
}
