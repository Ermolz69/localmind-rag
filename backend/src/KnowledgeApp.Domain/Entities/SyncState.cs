using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class SyncState : Entity
{
    public string Scope { get; set; } = "default";
    public string? Cursor { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
}
