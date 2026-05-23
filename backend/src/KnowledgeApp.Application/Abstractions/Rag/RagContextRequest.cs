namespace KnowledgeApp.Application.Abstractions;

public sealed record RagContextRequest(Guid ConversationId, string Question, int Limit = 6);
