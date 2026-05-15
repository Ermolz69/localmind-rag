using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class ExactVectorSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Empty_List_When_Index_Is_Empty()
    {
        await using var database = await TestDatabase.CreateAsync();
        var search = new ExactVectorSearchService(database.Context);

        var results = await search.SearchAsync([1, 0], new VectorSearchOptions());

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Rank_Relevant_Chunk_Higher_Than_Irrelevant_Chunk()
    {
        await using var database = await TestDatabase.CreateAsync();
        var relevant = await AddEmbeddedChunkAsync(database.Context, "Relevant document", "Needle chunk", [1, 0]);
        var irrelevant = await AddEmbeddedChunkAsync(database.Context, "Irrelevant document", "Other chunk", [0, 1]);
        var search = new ExactVectorSearchService(database.Context);

        var results = await search.SearchAsync([1, 0], new VectorSearchOptions(Limit: 2));

        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(relevant.DocumentId, result.DocumentId);
                Assert.Equal(relevant.ChunkId, result.ChunkId);
                Assert.Equal("Relevant document", result.DocumentName);
                Assert.Equal("Needle chunk", result.Snippet);
                Assert.Equal(1, result.Score, precision: 6);
            },
            result =>
            {
                Assert.Equal(irrelevant.DocumentId, result.DocumentId);
                Assert.Equal(irrelevant.ChunkId, result.ChunkId);
                Assert.Equal(0, result.Score, precision: 6);
            });
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Only_Selected_Document_When_Document_Filter_Is_Set()
    {
        await using var database = await TestDatabase.CreateAsync();
        var selected = await AddEmbeddedChunkAsync(database.Context, "Selected document", "Selected chunk", [1, 0]);
        await AddEmbeddedChunkAsync(database.Context, "Other document", "Other chunk", [1, 0]);
        var search = new ExactVectorSearchService(database.Context);

        var results = await search.SearchAsync([1, 0], new VectorSearchOptions(DocumentId: selected.DocumentId));

        var result = Assert.Single(results);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Only_Selected_Bucket_When_Bucket_Filter_Is_Set()
    {
        await using var database = await TestDatabase.CreateAsync();
        var selectedBucketId = Guid.NewGuid();
        var selected = await AddEmbeddedChunkAsync(database.Context, "Selected bucket document", "Selected bucket chunk", [1, 0], selectedBucketId);
        await AddEmbeddedChunkAsync(database.Context, "Other bucket document", "Other bucket chunk", [1, 0], Guid.NewGuid());
        var search = new ExactVectorSearchService(database.Context);

        var results = await search.SearchAsync([1, 0], new VectorSearchOptions(BucketId: selectedBucketId));

        var result = Assert.Single(results);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
    }

    private static async Task<(Guid DocumentId, Guid ChunkId)> AddEmbeddedChunkAsync(
        AppDbContext context,
        string documentName,
        string chunkText,
        float[] vector,
        Guid? bucketId = null)
    {
        var document = new Document { BucketId = bucketId, Name = documentName, Status = DocumentStatus.Indexed };
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

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
