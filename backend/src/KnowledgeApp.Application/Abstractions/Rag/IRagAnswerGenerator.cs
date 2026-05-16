using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public interface IRagAnswerGenerator
{
    Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default);
}
