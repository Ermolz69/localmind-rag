using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class DocumentEmbeddingService(
    IEmbeddingGenerator embeddingGenerator,
    IDateTimeProvider dateTimeProvider) : IDocumentEmbeddingService
{
    public string ModelName => embeddingGenerator.ModelName;

    public async Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        List<DocumentEmbedding> embeddings = new List<DocumentEmbedding>(chunks.Count);

        foreach (DocumentChunk chunk in chunks)
        {
            float[] vector = await embeddingGenerator.GenerateAsync(chunk.Text, cancellationToken);

            embeddings.Add(new DocumentEmbedding
            {
                CreatedAt = dateTimeProvider.UtcNow,
                DocumentChunkId = chunk.Id,
                ModelName = embeddingGenerator.ModelName,
                Dimension = vector.Length,
                Embedding = EmbeddingVectorSerializer.ToBytes(vector)
            });
        }

        return embeddings;
    }
}
