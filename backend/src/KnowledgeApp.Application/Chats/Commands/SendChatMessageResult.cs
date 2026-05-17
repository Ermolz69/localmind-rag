using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Chats;

public sealed record SendChatMessageResult(RagAnswerDto Answer);
