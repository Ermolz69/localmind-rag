using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class UploadDocumentHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Document_File_And_IngestionJob()
    {
        await using var database = await TestDatabase.CreateAsync();
        var storage = new FakeFileStorageService();
        var handler = new UploadDocumentHandler(database.Context, storage, new FixedDateTimeProvider());
        await using var content = new MemoryStream("hello localmind"u8.ToArray());

        var response = await handler.HandleAsync(new UploadDocumentCommand(content, "notes.txt", "text/plain", content.Length, null));

        var document = await database.Context.Documents.SingleAsync();
        var documentFile = await database.Context.DocumentFiles.SingleAsync();
        var ingestionJob = await database.Context.IngestionJobs.SingleAsync();

        Assert.Equal(document.Id, response.DocumentId);
        Assert.Equal(ingestionJob.Id, response.IngestionJobId);
        Assert.Equal(DocumentStatus.Queued.ToString(), response.Status);
        Assert.Equal(DocumentStatus.Queued, document.Status);
        Assert.Equal(SyncStatus.LocalOnly, document.SyncStatus);
        Assert.Equal(document.Id, documentFile.DocumentId);
        Assert.Equal(FileType.PlainText, documentFile.FileType);
        Assert.Equal(document.Id, ingestionJob.DocumentId);
        Assert.Equal(IngestionJobStatus.Queued, ingestionJob.Status);
        Assert.Equal(1, storage.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Empty_File()
    {
        await using var database = await TestDatabase.CreateAsync();
        var handler = new UploadDocumentHandler(database.Context, new FakeFileStorageService(), new FixedDateTimeProvider());
        await using var content = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new UploadDocumentCommand(content, "empty.txt", "text/plain", 0, null)));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public int SaveCalls { get; private set; }

        public Task<StoredFileDto> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(new StoredFileDto(fileName, $"runtime/app/files/{fileName}", content.Length, "HASH"));
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 12, 12, 0, 0, TimeSpan.Zero);
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
