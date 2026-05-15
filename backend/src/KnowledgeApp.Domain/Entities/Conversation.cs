using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class Conversation : Entity
{
    public string Title { get; set; } = "New chat";
    public Guid? LocalDeviceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
