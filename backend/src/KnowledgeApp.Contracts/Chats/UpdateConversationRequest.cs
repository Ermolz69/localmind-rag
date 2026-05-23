namespace KnowledgeApp.Contracts.Chats;

/// <summary>Request used to rename a chat conversation.</summary>
/// <param name="Title">Updated conversation title.</param>
public sealed record UpdateConversationRequest(string Title);
