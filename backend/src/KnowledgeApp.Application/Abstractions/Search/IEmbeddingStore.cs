using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IEmbeddingStore
{
    Task<IReadOnlyList<DocumentEmbedding>> GetEmbeddingsByChunkIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentEmbedding>> GetEmbeddingsForExactSearchAsync(CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyList<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IReadOnlyList<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default);
}
