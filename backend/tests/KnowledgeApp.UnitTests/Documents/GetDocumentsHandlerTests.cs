using KnowledgeApp.Application.Documents;
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
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Documents.AddRange(
            new Document { CreatedAt = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero), Name = "older.md", Status = DocumentStatus.Indexed },
            new Document { CreatedAt = new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero), Name = "newer.md", Status = DocumentStatus.Queued });
        await database.Context.SaveChangesAsync();
        var handler = new GetDocumentsHandler(database.Context);

        var documents = await handler.HandleAsync(new GetDocumentsQuery());

        Assert.Collection(
            documents,
            document => Assert.Equal("newer.md", document.Name),
            document => Assert.Equal("older.md", document.Name));
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Null_When_Document_Is_Missing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var handler = new GetDocumentByIdHandler(database.Context);

        var document = await handler.HandleAsync(new GetDocumentByIdQuery(Guid.NewGuid()));

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
