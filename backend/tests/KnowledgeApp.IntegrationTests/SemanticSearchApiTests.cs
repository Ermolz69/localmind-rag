using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class SemanticSearchApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SemanticSearchApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_Ranked_Sources()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddings = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();
        var query = $"semantic needle {Guid.NewGuid():N}";
        var queryVector = await embeddings.GenerateAsync(query);
        var relevant = await AddEmbeddedChunkAsync(db, "Relevant semantic document", "Semantic needle snippet", queryVector);
        await AddEmbeddedChunkAsync(db, "Irrelevant semantic document", "Unrelated semantic snippet", new float[queryVector.Length]);

        using var response = await client.PostAsJsonAsync("/api/search/semantic", new SemanticSearchRequest(query, Limit: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<RagSourceDto[]>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Equal(relevant.DocumentId, results[0].DocumentId);
        Assert.Equal(relevant.ChunkId, results[0].ChunkId);
        Assert.Equal("Semantic needle snippet", results[0].Snippet);
        Assert.True(results[0].Score > 0.99);
    }

    private static async Task<(Guid DocumentId, Guid ChunkId)> AddEmbeddedChunkAsync(
        AppDbContext context,
        string documentName,
        string chunkText,
        float[] vector)
    {
        var document = new Document { Name = documentName, Status = DocumentStatus.Indexed };
        var chunk = new DocumentChunk { DocumentId = document.Id, Index = 0, Text = chunkText };
        var embedding = new DocumentEmbedding
        {
            DocumentChunkId = chunk.Id,
            ModelName = "test-model",
            Dimension = vector.Length,
            Embedding = ToBytes(vector),
        };

        context.Documents.Add(document);
        context.DocumentChunks.Add(chunk);
        context.DocumentEmbeddings.Add(embedding);
        await context.SaveChangesAsync();

        return (document.Id, chunk.Id);
    }

    private static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
