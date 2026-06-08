using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class ExactVectorSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Empty_List_When_Index_Is_Empty()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        ExactVectorSearchService? search = new ExactVectorSearchService(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto>? results = await search.SearchAsync([1, 0], new VectorSearchOptions());

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Rank_Relevant_Chunk_Higher_Than_Irrelevant_Chunk()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        (Guid DocumentId, Guid ChunkId) relevant = await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Relevant document", "Needle chunk", [1, 0]);
        (Guid DocumentId, Guid ChunkId) irrelevant = await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Irrelevant document", "Other chunk", [0, 1]);
        ExactVectorSearchService? search = new ExactVectorSearchService(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto>? results = await search.SearchAsync([1, 0], new VectorSearchOptions(Limit: 2));

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
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        (Guid DocumentId, Guid ChunkId) selected = await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Selected document", "Selected chunk", [1, 0]);
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Other document", "Other chunk", [1, 0]);
        ExactVectorSearchService? search = new ExactVectorSearchService(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto>? results = await search.SearchAsync([1, 0], new VectorSearchOptions(DocumentId: selected.DocumentId));

        Contracts.Rag.RagSourceDto? result = Assert.Single(results);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Only_Selected_Bucket_When_Bucket_Filter_Is_Set()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        Guid selectedBucketId = Guid.NewGuid();
        (Guid DocumentId, Guid ChunkId) selected = await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Selected bucket document", "Selected bucket chunk", [1, 0], selectedBucketId);
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Other bucket document", "Other bucket chunk", [1, 0], Guid.NewGuid());
        ExactVectorSearchService? search = new ExactVectorSearchService(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto>? results = await search.SearchAsync([1, 0], new VectorSearchOptions(BucketId: selectedBucketId));

        Contracts.Rag.RagSourceDto? result = Assert.Single(results);
        Assert.Equal(selected.DocumentId, result.DocumentId);
        Assert.Equal(selected.ChunkId, result.ChunkId);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_List_When_Query_Vector_Is_Empty()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Document", "Chunk", [1, 0]);
        ExactVectorSearchService search = new(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto> results = await search.SearchAsync([], new VectorSearchOptions());

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_List_When_Limit_Is_Zero()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Document", "Chunk", [1, 0]);
        ExactVectorSearchService search = new(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto> results = await search.SearchAsync([1, 0], new VectorSearchOptions(Limit: 0));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Ignore_Embeddings_With_Different_Dimension()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Wrong dimension document", "Wrong dimension chunk", [1, 0, 0]);
        ExactVectorSearchService search = new(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto> results = await search.SearchAsync([1, 0], new VectorSearchOptions());

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Ignore_Deleted_Documents()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Deleted document", "Deleted chunk", [1, 0], deletedAt: DateTimeOffset.UtcNow);
        (Guid DocumentId, Guid ChunkId) visible = await EmbeddedChunkTestData.AddEmbeddedChunkAsync(database.Context, "Visible document", "Visible chunk", [1, 0]);
        ExactVectorSearchService search = new(database.Context);

        IReadOnlyList<Contracts.Rag.RagSourceDto> results = await search.SearchAsync([1, 0], new VectorSearchOptions(Limit: 2));

        Contracts.Rag.RagSourceDto result = Assert.Single(results);
        Assert.Equal(visible.DocumentId, result.DocumentId);
        Assert.Equal(visible.ChunkId, result.ChunkId);
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
            SqliteConnection? connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptions<AppDbContext>? options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            AppDbContext? context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase((SqliteConnection)connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
