namespace KnowledgeApp.Contracts.Rag;

/// <summary>Request used to send a user message into a RAG chat.</summary>
/// <param name="Content">User message text.</param>
public sealed record ChatMessageRequest(string Content);

