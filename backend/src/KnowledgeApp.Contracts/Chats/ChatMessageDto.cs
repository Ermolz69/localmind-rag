namespace KnowledgeApp.Contracts.Chats;

public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
