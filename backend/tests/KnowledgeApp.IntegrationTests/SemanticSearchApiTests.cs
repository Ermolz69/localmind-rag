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
        Assert.True(results.Sources[0].Score > 0);
    }

    [Fact]
    public async Task SemanticSearch_Should_Promote_Exact_Keyword_Match_From_FullText_Index()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmbeddingGenerator? embeddings =
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();
        IFullTextChunkIndex fullTextIndex =
            scope.ServiceProvider.GetRequiredService<IFullTextChunkIndex>();

        string token = $"UniqueToken{Guid.NewGuid():N}";
        float[] queryVector = await embeddings.GenerateAsync(token);

        await AddEmbeddedChunkAsync(
            db,
            "Vector-only document",
            "Semantically close but does not contain the token.",
            queryVector);

        (Guid DocumentId, Guid ChunkId) keywordMatch = await AddEmbeddedChunkAsync(
            db,
            "Keyword document",
            $"Use {token} when calling protected local endpoints.",
            new float[queryVector.Length]);

        await fullTextIndex.SyncDocumentAsync(keywordMatch.DocumentId);

        using HttpResponseMessage? response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(token, Limit: 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results =
            await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);
        Assert.NotEmpty(results.Sources);
        Assert.Equal(keywordMatch.DocumentId, results.Sources[0].DocumentId);
        Assert.Equal(keywordMatch.ChunkId, results.Sources[0].ChunkId);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_Only_Selected_Bucket_When_Bucket_Filter_Is_Set()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmbeddingGenerator? embeddings =
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();

        string? query = $"bucket scoped semantic {Guid.NewGuid():N}";
        float[]? queryVector = await embeddings.GenerateAsync(query);
        Guid selectedBucketId = Guid.NewGuid();

        (Guid DocumentId, Guid ChunkId) selected =
            await AddEmbeddedChunkAsync(
                db,
                "Selected bucket semantic document",
                "Selected bucket snippet",
                queryVector,
                selectedBucketId);

        await AddEmbeddedChunkAsync(
            db,
            "Other bucket semantic document",
            "Other bucket snippet",
            queryVector,
            Guid.NewGuid());

        using HttpResponseMessage? response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(query, Limit: 5, BucketId: selectedBucketId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results =
            await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);
        RagSourceDto result = Assert.Single(results.Sources);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_All_Buckets_When_Bucket_Filter_Is_Not_Set()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmbeddingGenerator? embeddings =
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();

        string? query = $"global semantic {Guid.NewGuid():N}";
        float[]? queryVector = await embeddings.GenerateAsync(query);

        (Guid DocumentId, Guid ChunkId) first =
            await AddEmbeddedChunkAsync(
                db,
                "First global semantic document",
                "First global snippet",
                queryVector,
                Guid.NewGuid());

        (Guid DocumentId, Guid ChunkId) second =
            await AddEmbeddedChunkAsync(
                db,
                "Second global semantic document",
                "Second global snippet",
                queryVector,
                Guid.NewGuid());

        using HttpResponseMessage? response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(query, Limit: 5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results =
            await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);
        Assert.Contains(results.Sources, result => result.DocumentId == first.DocumentId);
        Assert.Contains(results.Sources, result => result.DocumentId == second.DocumentId);
    }

    [Fact]
    public async Task SemanticSearch_Should_Return_Only_Documents_Inside_Date_Range()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmbeddingGenerator? embeddings =
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();

        string? query = $"dated semantic {Guid.NewGuid():N}";
        float[]? queryVector = await embeddings.GenerateAsync(query);
        DateTimeOffset selectedDate = new(2026, 06, 09, 12, 0, 0, TimeSpan.Zero);

        (Guid DocumentId, Guid ChunkId) selected =
            await AddEmbeddedChunkAsync(
                db,
                "Selected date semantic document",
                "Selected date snippet",
                queryVector,
                createdAt: selectedDate);

        await AddEmbeddedChunkAsync(
            db,
            "Older semantic document",
            "Older semantic snippet",
            queryVector,
            createdAt: selectedDate.AddDays(-7));

        using HttpResponseMessage? response =
            await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(
                    query,
                    Limit: 5,
                    DateFrom: selectedDate.Date,
                    DateTo: selectedDate.Date));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results =
            await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);
        RagSourceDto result = Assert.Single(results.Sources);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
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
        float[] vector,
        Guid? bucketId = null,
        DateTimeOffset? createdAt = null)
    {
        Document? document = new Document
        {
            BucketId = bucketId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
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
