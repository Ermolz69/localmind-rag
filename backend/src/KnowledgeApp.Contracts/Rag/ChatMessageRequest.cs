using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.Contracts.Rag;

/// <summary>Request used to send a user message into a RAG chat.</summary>
/// <param name="Content">User message text.</param>
/// <param name="Filters">Optional retrieval filters for this message.</param>
public sealed record ChatMessageRequest(string Content, RetrievalFilters? Filters = null);

