using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class UploadDocumentHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Document_File_And_IngestionJob()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        FakeFileStorageService? storage = new FakeFileStorageService();
        UploadDocumentHandler? handler = CreateHandler(database, storage);
        await using MemoryStream? content = new MemoryStream("hello localmind"u8.ToArray());

        UploadDocumentResponse? response = (await handler.HandleAsync(new UploadDocumentCommand(content, "notes.txt", "text/plain", content.Length, null))).AssertSuccess();

        Document? document = await database.Context.Documents.SingleAsync();
        DocumentFile? documentFile = await database.Context.DocumentFiles.SingleAsync();
        IngestionJob? ingestionJob = await database.Context.IngestionJobs.SingleAsync();

        Assert.Equal(document.Id, response.DocumentId);
        Assert.Equal(ingestionJob.Id, response.IngestionJobId);
        Assert.Equal(DocumentStatus.Queued.ToString(), response.Status);
        Assert.Equal(DocumentStatus.Queued, document.Status);
        Assert.Equal(SyncStatus.LocalOnly, document.SyncStatus);
        Assert.Equal(document.Id, documentFile.DocumentId);
        Assert.Equal(FileType.PlainText, documentFile.FileType);
        Assert.Contains($"runtime/app/files/{document.Id}/notes.txt", documentFile.LocalPath, StringComparison.Ordinal);
        Assert.Equal(document.Id, ingestionJob.DocumentId);
        Assert.Equal(IngestionJobStatus.Pending, ingestionJob.Status);
        Assert.Equal(0, ingestionJob.ProgressPercent);
        Assert.Equal("Pending", ingestionJob.CurrentStep);
        Assert.Equal(1, storage.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Default_Bucket_When_Bucket_Is_Not_Provided()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream("bucket me"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "default.md", "text/markdown", content.Length, null));

        Bucket? bucket = await database.Context.Buckets.SingleAsync();
        Document? document = await database.Context.Documents.SingleAsync();
        Assert.Equal(BucketConstants.DefaultBucketName, bucket.Name);
        Assert.Equal(bucket.Id, document.BucketId);
    }

    [Fact]
    public async Task HandleAsync_Should_Assign_Requested_Bucket_And_Save_Last_Selected_Bucket()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        Bucket? bucket = new Bucket { Name = "Research" };
        database.Context.Buckets.Add(bucket);
        await database.Context.SaveChangesAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream("selected bucket"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "paper.pdf", "application/pdf", content.Length, bucket.Id));

        Document? document = await database.Context.Documents.SingleAsync();
        AppSetting? setting = await database.Context.AppSettings.SingleAsync(x => x.Key == BucketSettingsKeys.LastSelectedBucketId);
        Assert.Equal(bucket.Id, document.BucketId);
        Assert.Equal(bucket.Id.ToString(), setting.Value);
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Last_Selected_Bucket_When_Bucket_Is_Not_Provided()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        Bucket? lastSelectedBucket = new Bucket { Name = "Inbox" };
        database.Context.Buckets.Add(lastSelectedBucket);
        database.Context.AppSettings.Add(new AppSetting
        {
            Key = BucketSettingsKeys.LastSelectedBucketId,
            Value = lastSelectedBucket.Id.ToString(),
        });
        await database.Context.SaveChangesAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream("last selected"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "last.txt", "text/plain", content.Length, null));

        Document? document = await database.Context.Documents.SingleAsync();
        Assert.Equal(lastSelectedBucket.Id, document.BucketId);
        Assert.Empty(await database.Context.Buckets.Where(x => x.Name == BucketConstants.DefaultBucketName).ToArrayAsync());
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Missing_Requested_Bucket()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream("missing bucket"u8.ToArray());

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "missing.txt", "text/plain", content.Length, Guid.NewGuid()));

        ApplicationError error = result.AssertFailure(ErrorType.NotFound);
        Assert.Equal(ErrorMessages.Buckets.NotFound, error.Message);
        Assert.Equal(ErrorCodes.Buckets.NotFound, error.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Empty_File()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream();

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "empty.txt", "text/plain", 0, null));
        Assert.Equal(ErrorCodes.Documents.FileEmpty, result.AssertFailure(ErrorType.Validation).Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Unsupported_Extension()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream("nope"u8.ToArray());

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "archive.zip", "application/zip", content.Length, null));
        Assert.Equal(ErrorCodes.Documents.UnsupportedFileType, result.AssertFailure(ErrorType.Validation).Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Too_Large_File()
    {
        await using TestDatabase? database = await TestDatabase.CreateAsync();
        UploadDocumentHandler? handler = CreateHandler(database);
        await using MemoryStream? content = new MemoryStream([1]);

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "large.txt", "text/plain", UploadDocumentCommandValidator.MaxFileSizeBytes + 1, null));
        Assert.Equal(ErrorCodes.Documents.FileTooLarge, result.AssertFailure(ErrorType.Validation).Code);
    }

    private static UploadDocumentHandler CreateHandler(TestDatabase database, FakeFileStorageService? storage = null)
    {
        FixedDateTimeProvider? clock = new FixedDateTimeProvider();
        return new UploadDocumentHandler(
            database.Context,
            storage ?? new FakeFileStorageService(),
            clock,
            new BucketResolver(database.Context, clock),
            new FakeLocalDeviceResolver(),
            new IngestionJobRepository(database.Context),
            new UploadDocumentCommandValidator());
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public int SaveCalls { get; private set; }

        public Task<StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(new StoredFileDto(fileName, $"runtime/app/files/{documentId}/{fileName}", content.Length, "HASH"));
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
