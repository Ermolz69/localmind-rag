using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobProcessorTests : IAsyncDisposable
{
    private readonly List<string> filesToDelete = [];

    [Fact]
    public async Task ProcessAsync_Should_Index_Text_Document_And_Complete_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "notes.txt", FileType.PlainText, "First paragraph.\n\nSecond paragraph.");
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == document.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
        Assert.Null(job.LastError);
        Assert.NotNull(job.ProcessedAt);
    }

    [Fact]
    public async Task ProcessAsync_Should_Remove_Existing_Chunks_On_Reindex()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "notes.md", FileType.Markdown, "New content.");
        database.Context.DocumentChunks.Add(new DocumentChunk
        {
            DocumentId = document.DocumentId,
            Index = 0,
            Text = "Old content.",
        });
        await database.Context.SaveChangesAsync();
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == document.DocumentId)
            .ToArrayAsync();

        Assert.Single(chunks);
        Assert.Equal("New content.", chunks[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Unsupported_Pdf_Document()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "paper.pdf", FileType.Pdf, "%PDF skeleton");
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);
        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Contains("not supported", job.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_Should_Throw_When_Job_Is_Missing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var processor = CreateProcessor(database);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(Guid.NewGuid()));

        Assert.Equal("Ingestion job was not found.", exception.Message);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var filePath in filesToDelete)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await Task.CompletedTask;
    }

    private static IngestionJobProcessor CreateProcessor(TestDatabase database)
    {
        var rawExtractor = new RawTextExtractor();
        return new IngestionJobProcessor(
            database.Context,
            new DocumentTextExtractorFactory(rawExtractor, new HtmlTextExtractor()),
            new SimpleDocumentChunker(),
            new FixedDateTimeProvider());
    }

    private async Task<(Guid DocumentId, Guid JobId)> CreateDocumentWithJobAsync(TestDatabase database, string fileName, FileType fileType, string content)
    {
        var document = new Document
        {
            Name = fileName,
            Status = DocumentStatus.Queued,
        };
        var filePath = Path.Combine(Path.GetTempPath(), $"localmind-ingestion-{Guid.NewGuid():N}-{fileName}");
        await File.WriteAllTextAsync(filePath, content);
        filesToDelete.Add(filePath);

        var documentFile = new DocumentFile
        {
            DocumentId = document.Id,
            FileName = fileName,
            FileType = fileType,
            LocalPath = filePath,
            SizeBytes = content.Length,
        };
        var job = new IngestionJob
        {
            DocumentId = document.Id,
            Status = IngestionJobStatus.Queued,
        };

        database.Context.Documents.Add(document);
        database.Context.DocumentFiles.Add(documentFile);
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        return (document.Id, job.Id);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 13, 12, 0, 0, TimeSpan.Zero);
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
