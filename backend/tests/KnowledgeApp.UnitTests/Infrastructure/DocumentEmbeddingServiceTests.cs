using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class DocumentEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateAsync_Should_Batch_Chunks_And_Preserve_Order()
    {
        BatchGenerator generator = new();
        DocumentEmbeddingService service = new(
            generator,
            new FixedDateTimeProvider(),
            Options.Create(new EmbeddingOptions { EmbeddingBatchSize = 2 }));

        DocumentChunk[] chunks =
        [
            new() { Id = Guid.NewGuid(), Text = "one" },
            new() { Id = Guid.NewGuid(), Text = "two" },
            new() { Id = Guid.NewGuid(), Text = "three" },
        ];

        IReadOnlyList<DocumentEmbedding> embeddings = await service.GenerateAsync(chunks);

        Assert.Equal(2, generator.BatchSizes.Count);
        Assert.Equal([2, 1], generator.BatchSizes);
        Assert.Equal(chunks.Select(chunk => chunk.Id), embeddings.Select(embedding => embedding.DocumentChunkId));
    }

    [Fact]
    public async Task GenerateAsync_Should_Fallback_To_Single_Generator()
    {
        SingleGenerator generator = new();
        DocumentEmbeddingService service = new(generator, new FixedDateTimeProvider());

        DocumentChunk[] chunks =
        [
            new() { Id = Guid.NewGuid(), Text = "one" },
            new() { Id = Guid.NewGuid(), Text = "two" },
        ];

        IReadOnlyList<DocumentEmbedding> embeddings = await service.GenerateAsync(chunks);

        Assert.Equal(2, generator.CallCount);
        Assert.Equal(2, embeddings.Count);
    }

    [Fact]
    public async Task GenerateAsync_Should_Normalize_Vectors()
    {
        BatchGenerator generator = new();
        DocumentEmbeddingService service = new(generator, new FixedDateTimeProvider());

        DocumentChunk[] chunks =
        [
            new() { Id = Guid.NewGuid(), Text = "unnormalized_text_that_produces_large_vector" }
        ];

        IReadOnlyList<DocumentEmbedding> embeddings = await service.GenerateAsync(chunks);

        Assert.Single(embeddings);

        var span = embeddings[0].Embedding.AsSpan();
        var vector = MemoryMarshal.Cast<byte, float>(span);

        // Verify the norm is approximately 1.0
        float norm = System.Numerics.Tensors.TensorPrimitives.Norm(vector);
        Assert.True(Math.Abs(norm - 1.0f) < 0.001f, $"Expected norm 1.0, got {norm}");
    }

    private sealed class BatchGenerator : IBatchEmbeddingGenerator
    {
        public string ModelName => "batch";

        public List<int> BatchSizes { get; } = [];

        public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new[] { (float)text.Length });
        }

        public Task<IReadOnlyList<float[]>> GenerateBatchAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default)
        {
            BatchSizes.Add(texts.Count);
            return Task.FromResult<IReadOnlyList<float[]>>(
                texts.Select(text => new[] { (float)text.Length }).ToArray());
        }
    }

    private sealed class SingleGenerator : IEmbeddingGenerator
    {
        public string ModelName => "single";

        public int CallCount { get; private set; }

        public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new[] { (float)text.Length });
        }
    }
}
