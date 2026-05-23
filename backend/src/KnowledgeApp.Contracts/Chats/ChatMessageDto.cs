namespace KnowledgeApp.Contracts.Chats;

/// <summary>Chat message returned from a conversation.</summary>
/// <param name="Id">Local message identifier.</param>
/// <param name="ConversationId">Conversation that owns the message.</param>
/// <param name="Role">Message role, such as user or assistant.</param>
/// <param name="Content">Message text content.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest update, when available.</param>
public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
