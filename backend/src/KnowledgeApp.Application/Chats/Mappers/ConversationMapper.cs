using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

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

    public static ChatMessageDto ToDto(ChatMessage message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.Role.ToString(),
            message.Content,
            message.CreatedAt,
            message.UpdatedAt);
    }
}
