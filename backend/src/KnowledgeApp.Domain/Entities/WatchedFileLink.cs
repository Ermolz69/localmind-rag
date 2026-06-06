using KnowledgeApp.Domain.Common;

namespace KnowledgeApp.Domain.Entities;

public sealed class WatchedFileLink : Entity
{
    public Guid DocumentId { get; set; }

    public string WatchedFolderPath { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string NormalizedFilePath { get; set; } = string.Empty;

    public string LastContentHash { get; set; } = string.Empty;

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
