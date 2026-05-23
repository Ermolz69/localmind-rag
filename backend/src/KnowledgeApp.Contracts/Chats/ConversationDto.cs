namespace KnowledgeApp.Contracts.Chats;

/// <summary>Chat conversation metadata.</summary>
/// <param name="Id">Local conversation identifier.</param>
/// <param name="Title">Conversation title shown in the UI.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest update, when available.</param>
public sealed record ConversationDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
