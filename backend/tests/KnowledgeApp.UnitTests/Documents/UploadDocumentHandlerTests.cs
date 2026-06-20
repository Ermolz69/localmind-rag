using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests.TestSupport.Fakes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class UploadDocumentHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Document_File_And_IngestionJob()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        FakeFileStorageService storage = new FakeFileStorageService();
        UploadDocumentHandler handler = CreateHandler(database, out FakeDomainEventPublisher publisher, storage);
        await using MemoryStream content = new MemoryStream("hello localmind"u8.ToArray());

        UploadDocumentResponse response = (await handler.HandleAsync(new UploadDocumentCommand(content, "notes.txt", "text/plain", content.Length, null))).AssertSuccess();

        Document document = await database.Context.Documents.SingleAsync();
        DocumentFile documentFile = await database.Context.DocumentFiles.SingleAsync();

        Assert.Equal(document.Id, response.DocumentId);
        Assert.Null(response.IngestionJobId);
        Assert.Equal(DocumentStatus.Queued.ToString(), response.Status);
        Assert.Equal(DocumentStatus.Queued, document.Status);
        Assert.Equal(SyncStatus.LocalOnly, document.SyncStatus);
        Assert.Equal(document.Id, documentFile.DocumentId);
        Assert.Equal(FileType.PlainText, documentFile.FileType);
        Assert.Contains($"runtime/app/files/{document.Id}/notes.txt", documentFile.LocalPath, StringComparison.Ordinal);

        Assert.Single(publisher.PublishedEvents);
        Assert.IsType<DocumentUploadedEvent>(publisher.PublishedEvents.Single());
        Assert.Equal(document.Id, ((DocumentUploadedEvent)publisher.PublishedEvents.Single()).DocumentId);
        Assert.Equal(1, storage.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Default_Bucket_When_Bucket_Is_Not_Provided()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream("bucket me"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "default.md", "text/markdown", content.Length, null));

        Bucket bucket = await database.Context.Buckets.SingleAsync();
        Document document = await database.Context.Documents.SingleAsync();
        Assert.Equal(BucketConstants.DefaultBucketName, bucket.Name);
        Assert.Equal(bucket.Id, document.BucketId);
    }

    [Fact]
    public async Task HandleAsync_Should_Assign_Requested_Bucket_And_Save_Last_Selected_Bucket()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        Bucket bucket = new Bucket { Name = "Research" };
        database.Context.Buckets.Add(bucket);
        await database.Context.SaveChangesAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream("selected bucket"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "paper.pdf", "application/pdf", content.Length, bucket.Id));

        Document document = await database.Context.Documents.SingleAsync();
        AppSetting setting = await database.Context.AppSettings.SingleAsync(x => x.Key == BucketSettingsKeys.LastSelectedBucketId);
        Assert.Equal(bucket.Id, document.BucketId);
        Assert.Equal(bucket.Id.ToString(), setting.Value);
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Last_Selected_Bucket_When_Bucket_Is_Not_Provided()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        Bucket lastSelectedBucket = new Bucket { Name = "Inbox" };
        database.Context.Buckets.Add(lastSelectedBucket);
        database.Context.AppSettings.Add(new AppSetting
        {
            Key = BucketSettingsKeys.LastSelectedBucketId,
            Value = lastSelectedBucket.Id.ToString(),
        });
        await database.Context.SaveChangesAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream("last selected"u8.ToArray());

        await handler.HandleAsync(new UploadDocumentCommand(content, "last.txt", "text/plain", content.Length, null));

        Document document = await database.Context.Documents.SingleAsync();
        Assert.Equal(lastSelectedBucket.Id, document.BucketId);
        Assert.Empty(await database.Context.Buckets.Where(x => x.Name == BucketConstants.DefaultBucketName).ToArrayAsync());
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Missing_Requested_Bucket()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream("missing bucket"u8.ToArray());

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "missing.txt", "text/plain", content.Length, Guid.NewGuid()));

        ApplicationError error = result.AssertFailure(ErrorType.NotFound);
        Assert.Equal(ErrorMessages.Buckets.NotFound, error.Message);
        Assert.Equal(ErrorCodes.Buckets.NotFound, error.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Empty_File()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream();

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "empty.txt", "text/plain", 0, null));
        Assert.Equal(ErrorCodes.Documents.FileEmpty, result.AssertFailure(ErrorType.Validation).Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Unsupported_Extension()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream("nope"u8.ToArray());

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "archive.zip", "application/zip", content.Length, null));
        Assert.Equal(ErrorCodes.Documents.UnsupportedFileType, result.AssertFailure(ErrorType.Validation).Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Too_Large_File()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        UploadDocumentHandler handler = CreateHandler(database, out _);
        await using MemoryStream content = new MemoryStream([1]);

        Result<UploadDocumentResponse> result = await handler.HandleAsync(
            new UploadDocumentCommand(content, "large.txt", "text/plain", UploadDocumentCommandValidator.MaxFileSizeBytes + 1, null));
        Assert.Equal(ErrorCodes.Documents.FileTooLarge, result.AssertFailure(ErrorType.Validation).Code);
    }

    private static UploadDocumentHandler CreateHandler(ApplicationTestDatabase database, out FakeDomainEventPublisher publisher, FakeFileStorageService? storage = null)
    {
        FixedDateTimeProvider clock = new FixedDateTimeProvider();
        var documentRepository = new KnowledgeApp.Infrastructure.Services.Persistence.DocumentRepository(database.Context);
        var bucketRepository = new KnowledgeApp.Infrastructure.Services.Persistence.BucketRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        publisher = new FakeDomainEventPublisher();
        return new UploadDocumentHandler(
            documentRepository,
            unitOfWork,
            storage ?? new FakeFileStorageService(),
            clock,
            new BucketResolver(bucketRepository, database.Context, clock),
            new FakeLocalDeviceResolver(),
            publisher,
            new UploadDocumentCommandValidator(),
            new FakeOperationLogRepository());
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public int SaveCalls { get; private set; }

        public Task<StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(new StoredFileDto(fileName, $"runtime/app/files/{documentId}/{fileName}", content.Length, "HASH"));
        }

        public Task DeleteAsync(string localPath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
