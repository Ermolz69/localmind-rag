using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class LocalDevice : Entity
{
    public string DeviceKey { get; set; } = string.Empty;
    public string Name { get; set; } = Environment.MachineName;
}

public sealed class Bucket : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
}

public sealed class Document : Entity
{
    public Guid? BucketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
}

public sealed class DocumentFile : Entity
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public FileType FileType { get; set; } = FileType.Unknown;
    public long SizeBytes { get; set; }
}

public sealed class DocumentChunk : Entity
{
    public Guid DocumentId { get; set; }
    public int Index { get; set; }
    public int? PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class DocumentEmbedding : Entity
{
    public Guid DocumentChunkId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public byte[] Embedding { get; set; } = [];
}

public sealed class Note : Entity
{
    public Guid? BucketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
}

public sealed class NoteLink : Entity
{
    public Guid SourceNoteId { get; set; }
    public Guid TargetNoteId { get; set; }
}

public sealed class Conversation : Entity
{
    public string Title { get; set; } = "New chat";
}

public sealed class ChatMessage : Entity
{
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class IngestionJob : Entity
{
    public Guid DocumentId { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Queued;
    public string? LastError { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

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

public sealed class SyncState : Entity
{
    public string Scope { get; set; } = "default";
    public string? Cursor { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
}

public sealed class AppSetting : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class AiModel : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public sealed class AiRuntime : Entity
{
    public AiProviderType Provider { get; set; } = AiProviderType.LlamaCpp;
    public AiRuntimeStatus Status { get; set; } = AiRuntimeStatus.Unknown;
    public string BaseUrl { get; set; } = string.Empty;
}
