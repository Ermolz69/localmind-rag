using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeApp.IntegrationTests;

public sealed class SemanticSearchApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public SemanticSearchApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_Ranked_Sources()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmbeddingGenerator? embeddings =
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();

        string? query = $"semantic needle {Guid.NewGuid():N}";
        float[]? queryVector = await embeddings.GenerateAsync(query);

        (Guid DocumentId, Guid ChunkId) relevant =
            await AddEmbeddedChunkAsync(
                db,
                "Relevant semantic document",
                "Semantic needle snippet",
                queryVector);

        await AddEmbeddedChunkAsync(
            db,
            "Irrelevant semantic document",
            "Unrelated semantic snippet",
            new float[queryVector.Length]);

        using HttpResponseMessage? response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(query, Limit: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results =
            await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);
        Assert.NotEmpty(results.Sources);
        Assert.Equal(relevant.DocumentId, results.Sources[0].DocumentId);
        Assert.Equal(relevant.ChunkId, results.Sources[0].ChunkId);
        Assert.Equal("Semantic needle snippet", results.Sources[0].Snippet);
        Assert.True(results.Sources[0].Score > 0.99);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_ValidationProblemDetails_For_Blank_Query()
    {
        using HttpClient? client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(" "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal(ErrorCodes.Search.ValidationFailed, envelope.Error!.Code);

        Assert.Contains(
            envelope.Error.Details ?? [],
            detail =>
                detail.Field == SemanticSearchRequestValidator.QueryField
                && detail.Message == ErrorMessages.Search.QueryRequired);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_ValidationProblemDetails_For_Invalid_Limit()
    {
        using HttpClient? client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest("query", Limit: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal(ErrorCodes.Search.ValidationFailed, envelope.Error!.Code);

        Assert.Contains(
            envelope.Error.Details ?? [],
            detail =>
                detail.Field == SemanticSearchRequestValidator.LimitField
                && detail.Message == ErrorMessages.Search.LimitOutOfRange);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_UnexpectedProblemDetails_When_Embedding_Fails()
    {
        using LocalApiTestFactory baseFactory = new();

        using WebApplicationFactory<Program> failingFactory =
            baseFactory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IEmbeddingGenerator>();
                    services.AddSingleton<IEmbeddingGenerator, FailingEmbeddingGenerator>();
                }));

        using HttpClient? client = failingFactory.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest("query"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal(ErrorCodes.Unexpected, envelope.Error!.Code);
    }

    private static async Task<(Guid DocumentId, Guid ChunkId)> AddEmbeddedChunkAsync(
        AppDbContext context,
        string documentName,
        string chunkText,
        float[] vector)
    {
        Document? document = new Document
        {
            Name = documentName,
            Status = DocumentStatus.Indexed,
        };

        DocumentChunk? chunk = new DocumentChunk
        {
            DocumentId = document.Id,
            Index = 0,
            Text = chunkText,
        };

        DocumentEmbedding? embedding = new DocumentEmbedding
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
        byte[]? bytes = new byte[vector.Length * sizeof(float)];

        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

        return bytes;
    }

    private sealed class FailingEmbeddingGenerator : IEmbeddingGenerator
    {
        public string ModelName => "failing-test-model";

        public Task<float[]> GenerateAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Synthetic embedding failure.");
        }
    }
}
