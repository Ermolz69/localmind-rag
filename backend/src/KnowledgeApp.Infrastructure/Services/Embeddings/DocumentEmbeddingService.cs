using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class DocumentEmbeddingService(
    IEmbeddingGenerator embeddingGenerator,
    IDateTimeProvider dateTimeProvider,
    IOptions<EmbeddingOptions>? options = null,
    ILogger<DocumentEmbeddingService>? logger = null) : IDocumentEmbeddingService
{
    private readonly EmbeddingOptions options = options?.Value ?? new EmbeddingOptions();

    public string ModelName => embeddingGenerator.ModelName;

    public async Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return [];
        }

        if (embeddingGenerator is not IBatchEmbeddingGenerator batchEmbeddingGenerator)
        {
            return await GenerateSequentialAsync(chunks, cancellationToken);
        }

        List<DocumentEmbedding> embeddings = new(chunks.Count);
        int batchSize = Math.Clamp(options.EmbeddingBatchSize, 1, 128);

        for (int start = 0; start < chunks.Count; start += batchSize)
        {
            IReadOnlyList<DocumentChunk> batch = chunks
                .Skip(start)
                .Take(batchSize)
                .ToArray();

            Stopwatch stopwatch = Stopwatch.StartNew();
            IReadOnlyList<float[]> vectors = await batchEmbeddingGenerator.GenerateBatchAsync(
                batch.Select(chunk => chunk.Text).ToArray(),
                cancellationToken);
            stopwatch.Stop();

            if (vectors.Count != batch.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding provider returned {vectors.Count} vectors for {batch.Count} chunks.");
            }

            for (int index = 0; index < batch.Count; index++)
            {
                embeddings.Add(CreateEmbedding(batch[index], vectors[index]));
            }

            double chunksPerSecond = stopwatch.Elapsed.TotalSeconds <= 0
                ? batch.Count
                : batch.Count / stopwatch.Elapsed.TotalSeconds;
            double averageLatencyMs = stopwatch.Elapsed.TotalMilliseconds / batch.Count;

            logger?.LogInformation(
                "Generated embedding batch of {BatchSize} chunks in {ElapsedMilliseconds} ms ({ChunksPerSecond:F2} chunks/sec, {AverageLatencyMs:F2} ms/chunk).",
                batch.Count,
                stopwatch.ElapsedMilliseconds,
                chunksPerSecond,
                averageLatencyMs);
        }

        return embeddings;
    }

    private async Task<IReadOnlyList<DocumentEmbedding>> GenerateSequentialAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        List<DocumentEmbedding> embeddings = new(chunks.Count);

        foreach (DocumentChunk chunk in chunks)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            float[] vector = await embeddingGenerator.GenerateAsync(chunk.Text, cancellationToken);
            stopwatch.Stop();

            logger?.LogDebug(
                "Generated single embedding in {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            embeddings.Add(CreateEmbedding(chunk, vector));
        }

        return embeddings;
    }

    private DocumentEmbedding CreateEmbedding(DocumentChunk chunk, float[] vector)
    {
        return new DocumentEmbedding
        {
            CreatedAt = dateTimeProvider.UtcNow,
            DocumentChunkId = chunk.Id,
            ModelName = embeddingGenerator.ModelName,
            Dimension = vector.Length,
            Embedding = EmbeddingVectorSerializer.ToBytes(vector)
        };
    }
}
