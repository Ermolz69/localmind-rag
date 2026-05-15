using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Chats;

public static class ConversationMapper
{
    public static ConversationDto ToDto(Conversation conversation)
    {
        return new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt);
    }
}
