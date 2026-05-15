using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public interface IRagContextBuilder
{
    Task<IReadOnlyList<RagSourceDto>> BuildAsync(string question, CancellationToken cancellationToken = default);
}
