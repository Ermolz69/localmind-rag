using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class GetDocumentsHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Documents_In_CreatedAt_Descending_Order()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        database.Context.Documents.AddRange(
            new Document { CreatedAt = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero), Name = "older.md", Status = DocumentStatus.Indexed },
            new Document { CreatedAt = new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero), Name = "newer.md", Status = DocumentStatus.Queued });
        await database.Context.SaveChangesAsync();
        GetDocumentsHandler? handler = new GetDocumentsHandler(database.Context);

        CursorPage<DocumentDto> documents = await handler.HandleAsync(new GetDocumentsQuery());

        Assert.Collection(
            documents.Items,
            document => Assert.Equal("newer.md", document.Name),
            document => Assert.Equal("older.md", document.Name));
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Cursor_Page_Without_Duplicates()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        Document newest = new() { CreatedAt = new DateTimeOffset(2026, 5, 13, 12, 0, 0, TimeSpan.Zero), Name = "newest.md" };
        Document middle = new() { CreatedAt = new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero), Name = "middle.md" };
        Document oldest = new() { CreatedAt = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero), Name = "oldest.md" };
        database.Context.Documents.AddRange(newest, middle, oldest);
        await database.Context.SaveChangesAsync();
        GetDocumentsHandler handler = new(database.Context);

        CursorPage<DocumentDto> firstPage = await handler.HandleAsync(new GetDocumentsQuery(Limit: 2));
        CursorPage<DocumentDto> secondPage = await handler.HandleAsync(new GetDocumentsQuery(Cursor: firstPage.NextCursor, Limit: 2));

        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);
        Assert.Equal(["newest.md", "middle.md"], firstPage.Items.Select(document => document.Name).ToArray());
        Assert.False(secondPage.HasMore);
        Assert.Equal(["oldest.md"], secondPage.Items.Select(document => document.Name).ToArray());
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Null_When_Document_Is_Missing()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        GetDocumentByIdHandler? handler = new GetDocumentByIdHandler(database.Context);

        DocumentDto? document = await handler.HandleAsync(new GetDocumentByIdQuery(Guid.NewGuid()));

        Assert.Null(document);
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
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
