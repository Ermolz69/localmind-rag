namespace KnowledgeApp.Contracts.Chats;

public sealed record ConversationDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
