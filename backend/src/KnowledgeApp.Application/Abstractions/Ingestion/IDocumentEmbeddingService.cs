using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IDocumentEmbeddingService
{
    string ModelName => string.Empty;

    Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);
}
