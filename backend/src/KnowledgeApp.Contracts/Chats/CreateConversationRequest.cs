namespace KnowledgeApp.Contracts.Chats;

/// <summary>Request used to create a chat conversation.</summary>
/// <param name="Title">Initial conversation title.</param>
public sealed record CreateConversationRequest(string Title);
