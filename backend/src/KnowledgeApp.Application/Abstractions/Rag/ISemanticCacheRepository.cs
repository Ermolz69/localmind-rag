using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions.Rag;

public interface ISemanticCacheRepository
{
    Task<SemanticCacheEntry?> FindBestMatchAsync(float[] queryEmbedding, double threshold, CancellationToken ct);
    Task AddAsync(SemanticCacheEntry entry, CancellationToken ct);
}
