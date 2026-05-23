using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public interface IRagContextBuilder
{
    Task<RagContext> BuildAsync(RagContextRequest request, CancellationToken cancellationToken = default);
}
