using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.Application.Abstractions;

public interface IRagAnswerGenerator
{
    Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, RetrievalFilters? filters = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(Guid conversationId, string question, RetrievalFilters? filters = null, CancellationToken cancellationToken = default);
}
