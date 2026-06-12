using System.Diagnostics;
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
public class ExactVectorSearchPerformanceTests(RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private static float[] GenerateRandomVector(int dimension, Random random)
    {
        float[] vector = new float[dimension];
        float normSq = 0;
        for (int i = 0; i < dimension; i++)
        {
            vector[i] = (float)(random.NextDouble() * 2 - 1);
            normSq += vector[i] * vector[i];
        }
        float norm = MathF.Sqrt(normSq);
        for (int i = 0; i < dimension; i++)
        {
            vector[i] /= norm;
        }
        return vector;
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(50000)]
    public async Task Benchmark_ExactVectorSearch_Latency(int chunkCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure clean state
        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_embeddings");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM document_chunks");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM documents");

        var doc = new Document { Id = Guid.NewGuid(), Name = $"Synthetic_Doc_{chunkCount}", BucketId = Guid.NewGuid(), LocalVersion = 1 };
        db.Documents.Add(doc);

        var random = new Random(42);
        int dimension = 1024;

        // Batch inserting to avoid tracking massive graphs
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        Console.WriteLine($"Seeding {chunkCount} synthetic chunks...");
        for (int i = 0; i < chunkCount; i++)
        {
            var chunkId = Guid.NewGuid();
            var chunk = new DocumentChunk { Id = chunkId, DocumentId = doc.Id, Text = $"Chunk {i}", PageNumber = 1 };
            db.DocumentChunks.Add(chunk);

            var embedding = new DocumentEmbedding
            {
                DocumentChunkId = chunkId,
                Dimension = dimension,
                ModelName = "bge-m3-synthetic",
                Embedding = System.Runtime.InteropServices.MemoryMarshal.Cast<float, byte>(GenerateRandomVector(dimension, random).AsSpan()).ToArray()
            };
            db.DocumentEmbeddings.Add(embedding);

            if (i > 0 && i % 5000 == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.ChangeTracker.AutoDetectChangesEnabled = true;

        var searchService = new ExactVectorSearchService(db, NullLogger<ExactVectorSearchService>.Instance);
        var queryVector = GenerateRandomVector(dimension, random);
        var options = new KnowledgeApp.Application.Abstractions.VectorSearchOptions(Limit: 10);

        // Warmup
        await searchService.SearchAsync(queryVector, options, CancellationToken.None);

        int iterations = 5;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memBefore = GC.GetAllocatedBytesForCurrentThread();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await searchService.SearchAsync(queryVector, options, CancellationToken.None);
        }
        sw.Stop();

        long memAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = memAfter - memBefore;

        double avgLatencyMs = sw.Elapsed.TotalMilliseconds / iterations;
        double avgAllocMs = allocatedBytes / (double)iterations;

        Console.WriteLine($"[Benchmark] {chunkCount} chunks, Top-K: 10, Dim: 1024. Avg Latency: {avgLatencyMs:F2} ms, Avg Allocation: {avgAllocMs:F0} bytes");

        Assert.True(avgLatencyMs > 0);
    }
}
