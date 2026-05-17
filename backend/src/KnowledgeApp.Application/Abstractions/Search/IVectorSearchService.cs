using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public sealed record VectorSearchOptions(int Limit = 8, Guid? BucketId = null, Guid? DocumentId = null);

public interface IVectorSearchService
{
    Task<IReadOnlyList<RagSourceDto>> SearchAsync(
        float[] queryVector,
        VectorSearchOptions options,
        CancellationToken cancellationToken = default);
}
