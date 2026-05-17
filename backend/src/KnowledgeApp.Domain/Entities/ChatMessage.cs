using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class ChatMessage : Entity
{
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? LocalDeviceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
