using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services.WatchedFolders;
using KnowledgeApp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFolderCleanupServiceTests : IAsyncLifetime
{
    private TestDatabase database = null!;

    public async Task InitializeAsync()
    {
        database = await TestDatabase.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
    }

    [Fact]
    public async Task CleanupDeletedFilesAsync_ShouldRemoveDeletedLinksAndRelatedEntities()
    {
        // Arrange
        var dbContext = database.Context;
        var sut = new WatchedFolderCleanupService(dbContext, new FakeFileStorageService(), NullLogger<WatchedFolderCleanupService>.Instance);

        var doc1 = new Document { Id = Guid.NewGuid(), Name = "doc1", Status = DocumentStatus.Deleted, DeletedAt = DateTimeOffset.UtcNow };
        var docFile1 = new DocumentFile { Id = Guid.NewGuid(), DocumentId = doc1.Id, FileName = "doc1", FileType = FileType.PlainText, LocalPath = "c:/test/doc1" };
        var chunk1 = new DocumentChunk { Id = Guid.NewGuid(), DocumentId = doc1.Id, Text = "test" };
        var embed1 = new DocumentEmbedding { Id = Guid.NewGuid(), DocumentChunkId = chunk1.Id, Embedding = [], Dimension = 128 };
        var job1 = new IngestionJob { Id = Guid.NewGuid(), DocumentId = doc1.Id, Status = IngestionJobStatus.Pending };
        var link1 = new WatchedFileLink { Id = Guid.NewGuid(), DocumentId = doc1.Id, WatchedFolderPath = "c:/test", FilePath = "c:/test/doc1", NormalizedFilePath = "C:/TEST/DOC1", LastContentHash = "hash", DeletedAt = DateTimeOffset.UtcNow, LastSeenAt = DateTimeOffset.UtcNow };

        var doc2 = new Document { Id = Guid.NewGuid(), Name = "doc2", Status = DocumentStatus.Queued };
        var docFile2 = new DocumentFile { Id = Guid.NewGuid(), DocumentId = doc2.Id, FileName = "doc2", FileType = FileType.PlainText, LocalPath = "c:/test/doc2" };
        var chunk2 = new DocumentChunk { Id = Guid.NewGuid(), DocumentId = doc2.Id, Text = "test" };
        var embed2 = new DocumentEmbedding { Id = Guid.NewGuid(), DocumentChunkId = chunk2.Id, Embedding = [], Dimension = 128 };
        var job2 = new IngestionJob { Id = Guid.NewGuid(), DocumentId = doc2.Id, Status = IngestionJobStatus.Pending };
        var link2 = new WatchedFileLink { Id = Guid.NewGuid(), DocumentId = doc2.Id, WatchedFolderPath = "c:/test", FilePath = "c:/test/doc2", NormalizedFilePath = "C:/TEST/DOC2", LastContentHash = "hash", DeletedAt = null, LastSeenAt = DateTimeOffset.UtcNow };

        dbContext.Documents.AddRange(doc1, doc2);
        dbContext.DocumentFiles.AddRange(docFile1, docFile2);
        dbContext.DocumentChunks.AddRange(chunk1, chunk2);
        dbContext.DocumentEmbeddings.AddRange(embed1, embed2);
        dbContext.IngestionJobs.AddRange(job1, job2);
        dbContext.WatchedFileLinks.AddRange(link1, link2);
        await dbContext.SaveChangesAsync();

        // Act
        int result = await sut.CleanupDeletedFilesAsync();

        // Assert
        Assert.Equal(1, result);

        // Link1 should be deleted, Link2 should remain
        Assert.Null(await dbContext.WatchedFileLinks.FirstOrDefaultAsync(x => x.Id == link1.Id));
        Assert.NotNull(await dbContext.WatchedFileLinks.FirstOrDefaultAsync(x => x.Id == link2.Id));

        // Doc1 should be deleted, Doc2 should remain
        Assert.Null(await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == doc1.Id));
        Assert.NotNull(await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == doc2.Id));

        // DocFile1 should be deleted, DocFile2 should remain
        Assert.Null(await dbContext.DocumentFiles.FirstOrDefaultAsync(x => x.Id == docFile1.Id));
        Assert.NotNull(await dbContext.DocumentFiles.FirstOrDefaultAsync(x => x.Id == docFile2.Id));

        // Chunk1 should be deleted, Chunk2 should remain
        Assert.Null(await dbContext.DocumentChunks.FirstOrDefaultAsync(x => x.Id == chunk1.Id));
        Assert.NotNull(await dbContext.DocumentChunks.FirstOrDefaultAsync(x => x.Id == chunk2.Id));

        // Embed1 should be deleted, Embed2 should remain
        Assert.Null(await dbContext.DocumentEmbeddings.FirstOrDefaultAsync(x => x.Id == embed1.Id));
        Assert.NotNull(await dbContext.DocumentEmbeddings.FirstOrDefaultAsync(x => x.Id == embed2.Id));

        // Job1 should be deleted, Job2 should remain
        Assert.Null(await dbContext.IngestionJobs.FirstOrDefaultAsync(x => x.Id == job1.Id));
        Assert.NotNull(await dbContext.IngestionJobs.FirstOrDefaultAsync(x => x.Id == job2.Id));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection connection;

        private TestDatabase(Microsoft.Data.Sqlite.SqliteConnection connection, AppDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
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

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<KnowledgeApp.Contracts.Documents.StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new KnowledgeApp.Contracts.Documents.StoredFileDto(fileName, $"runtime/app/files/{documentId}/{fileName}", content.Length, "HASH"));
        }

        public Task DeleteAsync(string localPath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
