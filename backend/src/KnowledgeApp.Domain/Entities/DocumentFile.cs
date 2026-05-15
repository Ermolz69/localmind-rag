using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class DocumentFile : Entity
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public FileType FileType { get; set; } = FileType.Unknown;
    public long SizeBytes { get; set; }
}
