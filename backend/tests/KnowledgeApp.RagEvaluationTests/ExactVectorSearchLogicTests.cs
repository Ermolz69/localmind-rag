using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.RagEvaluationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KnowledgeApp.RagEvaluationTests;

[Collection("Sequential")]
public class ExactVectorSearchLogicTests(RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private static float[] Normalize(float[] vector)
    {
        float[] result = new float[vector.Length];
        vector.CopyTo(result, 0);
        float norm = System.Numerics.Tensors.TensorPrimitives.Norm(result);
        if (norm > 0)
        {
            System.Numerics.Tensors.TensorPrimitives.Divide(result, norm, result);
        }
        return result;
    }

    [Fact]
    public async Task Search_Handles_Corrupted_Blobs_Gracefully()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_embeddings");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_chunks");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM documents");

        var doc = new Document { Id = Guid.NewGuid(), Name = "Doc", BucketId = Guid.NewGuid(), LocalVersion = 1 };
        db.Documents.Add(doc);

        var chunkId1 = Guid.NewGuid();
        db.DocumentChunks.Add(new DocumentChunk { Id = chunkId1, DocumentId = doc.Id, Text = "Chunk 1", PageNumber = 1 });

        var chunkId2 = Guid.NewGuid();
        db.DocumentChunks.Add(new DocumentChunk { Id = chunkId2, DocumentId = doc.Id, Text = "Chunk 2", PageNumber = 1 });

        // Corrupted blobs (wrong dimension and bad byte length)
        db.DocumentEmbeddings.Add(new DocumentEmbedding
        {
            DocumentChunkId = chunkId1,
            Dimension = 10,
            ModelName = "test",
            Embedding = new byte[7] // not divisible by 4
        });
        db.DocumentEmbeddings.Add(new DocumentEmbedding
        {
            DocumentChunkId = chunkId2,
            Dimension = 1024,
            ModelName = "test",
            Embedding = new byte[0] // empty
        });

        await db.SaveChangesAsync();

        var searchService = new ExactVectorSearchService(db, NullLogger<ExactVectorSearchService>.Instance);
        var query = new float[1024];
        query[0] = 1;

        var results = await searchService.SearchAsync(query, new VectorSearchOptions(Limit: 5), CancellationToken.None);

        // Should return 0 results and not throw
        Assert.Empty(results);
    }

    [Fact]
    public async Task TopK_PriorityQueue_Maintains_Correct_Order_And_Handles_Duplicates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_embeddings");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_chunks");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM documents");

        var doc = new Document { Id = Guid.NewGuid(), Name = "Doc", BucketId = Guid.NewGuid(), LocalVersion = 1 };
        db.Documents.Add(doc);

        // Create query
        var query = new float[1024];
        query[0] = 1;

        // Create vectors with specific similarities
        // Dot product with [1, 0, ...] is just the first element.
        float[] scores = [0.1f, 0.9f, 0.4f, 0.9f, 0.8f, -0.2f];

        for (int i = 0; i < scores.Length; i++)
        {
            var chunkId = Guid.NewGuid();
            db.DocumentChunks.Add(new DocumentChunk { Id = chunkId, DocumentId = doc.Id, Text = $"Chunk {i}", PageNumber = 1 });

            var vector = new float[1024];
            vector[0] = scores[i];

            // To make it a valid normalized vector, we set the second element such that vector length is 1.
            // If score is 0.9, x^2 + y^2 = 1 => y = sqrt(1 - 0.9^2)
            if (Math.Abs(scores[i]) <= 1.0f)
            {
                vector[1] = (float)Math.Sqrt(1.0 - (scores[i] * scores[i]));
            }

            db.DocumentEmbeddings.Add(new DocumentEmbedding
            {
                DocumentChunkId = chunkId,
                Dimension = 1024,
                ModelName = "test",
                Embedding = System.Runtime.InteropServices.MemoryMarshal.Cast<float, byte>(vector.AsSpan()).ToArray()
            });
        }
        await db.SaveChangesAsync();

        var searchService = new ExactVectorSearchService(db, NullLogger<ExactVectorSearchService>.Instance);

        // We expect top 3 to be 0.9, 0.9, 0.8
        var results = await searchService.SearchAsync(query, new VectorSearchOptions(Limit: 3), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Equal(0.9, results[0].Score, 4);
        Assert.Equal(0.9, results[1].Score, 4);
        Assert.Equal(0.8, results[2].Score, 4);
    }
}
