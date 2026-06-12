using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services.WatchedFolders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFileIngestionServiceTests : IAsyncDisposable
{
    private readonly List<string> pathsToDelete = [];

    [Fact]
    public async Task HandleCreatedOrChangedAsync_Should_CreateDocumentFileLinkAndIngestionJob_ForNewWatchedFile()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string filePath = Path.Combine(watchedFolderPath, "watched-file.txt");

        await File.WriteAllTextAsync(filePath, "Initial watched file content.");

        var (service, provider, signal) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        Domain.Entities.Document document = await database.Context.Documents.SingleAsync();
        Domain.Entities.DocumentFile documentFile = await database.Context.DocumentFiles.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();
        Domain.Entities.IngestionJob job = await database.Context.IngestionJobs.SingleAsync();

        Assert.Equal("watched-file.txt", document.Name);
        Assert.Equal(DocumentStatus.Queued, document.Status);
        Assert.Equal(SyncStatus.LocalOnly, document.SyncStatus);

        Assert.Equal(document.Id, documentFile.DocumentId);
        Assert.Equal("watched-file.txt", documentFile.FileName);
        Assert.Equal(FileType.PlainText, documentFile.FileType);
        Assert.Equal(filePath, documentFile.LocalPath);
        Assert.False(string.IsNullOrWhiteSpace(documentFile.ContentHash));
        Assert.True(documentFile.SizeBytes > 0);

        Assert.Equal(document.Id, link.DocumentId);
        Assert.Equal(NormalizePath(filePath), link.NormalizedFilePath);
        Assert.Equal(documentFile.ContentHash, link.LastContentHash);
        Assert.Null(link.DeletedAt);

        Assert.Equal(document.Id, job.DocumentId);
        Assert.Equal(IngestionJobStatus.Pending, job.Status);
        Assert.Equal([job.Id], signal.PublishedJobIds);
    }

    [Fact]
    public async Task HandleCreatedOrChangedAsync_Should_CreateReindexJob_WhenExistingWatchedFileChanges()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string filePath = Path.Combine(watchedFolderPath, "watched-file.txt");

        await File.WriteAllTextAsync(filePath, "Initial watched file content.");

        var (service, provider, signal) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        Domain.Entities.Document documentBeforeUpdate = await database.Context.Documents.SingleAsync();
        Domain.Entities.DocumentFile documentFileBeforeUpdate = await database.Context.DocumentFiles.SingleAsync();
        string firstHash = documentFileBeforeUpdate.ContentHash;

        await File.WriteAllTextAsync(filePath, "Updated watched file content.");

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        Domain.Entities.Document documentAfterUpdate = await database.Context.Documents.SingleAsync();
        Domain.Entities.DocumentFile documentFileAfterUpdate = await database.Context.DocumentFiles.SingleAsync();
        Domain.Entities.WatchedFileLink linkAfterUpdate = await database.Context.WatchedFileLinks.SingleAsync();
        int jobCount = await database.Context.IngestionJobs.CountAsync();

        Assert.Equal(documentBeforeUpdate.Id, documentAfterUpdate.Id);
        Assert.Equal(DocumentStatus.Queued, documentAfterUpdate.Status);
        Assert.Null(documentAfterUpdate.DeletedAt);

        Assert.NotEqual(firstHash, documentFileAfterUpdate.ContentHash);
        Assert.Equal(documentFileAfterUpdate.ContentHash, linkAfterUpdate.LastContentHash);
        Assert.Null(linkAfterUpdate.DeletedAt);

        Assert.Equal(2, jobCount);
        Assert.Equal(2, signal.PublishedJobIds.Count);
    }

    [Fact]
    public async Task HandleCreatedOrChangedAsync_Should_NotCreateDuplicateJob_WhenContentHashDidNotChange()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string filePath = Path.Combine(watchedFolderPath, "watched-file.txt");

        await File.WriteAllTextAsync(filePath, "Same watched file content.");

        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        Domain.Entities.WatchedFileLink link1 = await database.Context.WatchedFileLinks.SingleAsync();
        DateTimeOffset firstSeenAt = link1.LastSeenAt!.Value;

        provider.UtcNow = provider.UtcNow.AddMinutes(5);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        int documentCount = await database.Context.Documents.CountAsync();
        int fileCount = await database.Context.DocumentFiles.CountAsync();
        int linkCount = await database.Context.WatchedFileLinks.CountAsync();
        int jobCount = await database.Context.IngestionJobs.CountAsync();

        Assert.Equal(1, documentCount);
        Assert.Equal(1, fileCount);
        Assert.Equal(1, linkCount);
        Assert.Equal(1, jobCount);

        Domain.Entities.WatchedFileLink link2 = await database.Context.WatchedFileLinks.SingleAsync();
        Assert.True(link2.LastSeenAt > firstSeenAt, "LastSeenAt should be updated even if ContentHash didn't change.");
    }

    [Fact]
    public async Task HandleDeletedAsync_Should_MarkRelatedDocumentAsDeleted()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string filePath = Path.Combine(watchedFolderPath, "watched-file.txt");

        await File.WriteAllTextAsync(filePath, "Initial watched file content.");

        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        File.Delete(filePath);

        await service.HandleDeletedAsync(filePath);

        Domain.Entities.Document document = await database.Context.Documents.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();
        int jobCount = await database.Context.IngestionJobs.CountAsync();

        Assert.Equal(DocumentStatus.Deleted, document.Status);
        Assert.Equal(SyncStatus.DeletedLocal, document.SyncStatus);
        Assert.NotNull(document.DeletedAt);

        Assert.NotNull(link.DeletedAt);

        Assert.Equal(1, jobCount);
    }

    [Fact]
    public async Task HandleCreatedOrChangedAsync_Should_IgnoreUnsupportedFiles()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string filePath = Path.Combine(watchedFolderPath, "unsupported.exe");

        await File.WriteAllTextAsync(filePath, "Unsupported file content.");

        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(filePath, watchedFolderPath);

        Assert.Empty(database.Context.Documents);
        Assert.Empty(database.Context.DocumentFiles);
        Assert.Empty(database.Context.WatchedFileLinks);
        Assert.Empty(database.Context.IngestionJobs);
    }

    [Fact]
    public async Task HandleRenamedAsync_Should_UpdateMetadataWithoutJob_WhenHashDidNotChange()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string oldFilePath = Path.Combine(watchedFolderPath, "old-name.txt");
        string newFilePath = Path.Combine(watchedFolderPath, "new-name.txt");

        await File.WriteAllTextAsync(oldFilePath, "Content.");
        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(oldFilePath, watchedFolderPath);

        Domain.Entities.Document document1 = await database.Context.Documents.SingleAsync();
        Guid docId = document1.Id;

        // Move the file and keep the same content
        File.Move(oldFilePath, newFilePath);

        await service.HandleRenamedAsync(oldFilePath, newFilePath, watchedFolderPath);

        int documentCount = await database.Context.Documents.CountAsync();
        int jobCount = await database.Context.IngestionJobs.CountAsync();
        Domain.Entities.Document document2 = await database.Context.Documents.SingleAsync();
        Domain.Entities.DocumentFile documentFile = await database.Context.DocumentFiles.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();

        Assert.Equal(1, documentCount);
        Assert.Equal(1, jobCount); // Only the initial job
        Assert.Equal(docId, document2.Id);
        Assert.Equal("new-name.txt", document2.Name);
        Assert.Equal("new-name.txt", documentFile.FileName);
        Assert.Equal(newFilePath, documentFile.LocalPath);
        Assert.Equal(newFilePath, link.FilePath);
        Assert.Equal(NormalizePath(newFilePath), link.NormalizedFilePath);
    }

    [Fact]
    public async Task HandleRenamedAsync_Should_UpdateMetadataAndCreateJob_WhenHashChanged()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string oldFilePath = Path.Combine(watchedFolderPath, "old-name.txt");
        string newFilePath = Path.Combine(watchedFolderPath, "new-name.txt");

        await File.WriteAllTextAsync(oldFilePath, "Old Content.");
        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(oldFilePath, watchedFolderPath);

        File.Move(oldFilePath, newFilePath);
        await File.WriteAllTextAsync(newFilePath, "New Content.");

        await service.HandleRenamedAsync(oldFilePath, newFilePath, watchedFolderPath);

        int jobCount = await database.Context.IngestionJobs.CountAsync();
        Domain.Entities.Document document = await database.Context.Documents.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();

        Assert.Equal(2, jobCount); // Initial + Renamed/Changed
        Assert.Equal("new-name.txt", document.Name);
        Assert.Equal(newFilePath, link.FilePath);
    }

    [Fact]
    public async Task HandleRenamedAsync_Should_DeleteOldDocument_WhenRenamedToUnsupportedType()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string oldFilePath = Path.Combine(watchedFolderPath, "old-name.txt");
        string newFilePath = Path.Combine(watchedFolderPath, "new-name.exe");

        await File.WriteAllTextAsync(oldFilePath, "Content.");
        var (service, provider, _) = CreateService(database);

        await service.HandleCreatedOrChangedAsync(oldFilePath, watchedFolderPath);

        File.Move(oldFilePath, newFilePath);

        await service.HandleRenamedAsync(oldFilePath, newFilePath, watchedFolderPath);

        Domain.Entities.Document document = await database.Context.Documents.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();

        Assert.Equal(DocumentStatus.Deleted, document.Status);
        Assert.NotNull(document.DeletedAt);
        Assert.NotNull(link.DeletedAt);
    }

    [Fact]
    public async Task HandleRenamedAsync_Should_CreateNewDocument_WhenOldLinkMissing()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        string watchedFolderPath = CreateTempDirectory();
        string oldFilePath = Path.Combine(watchedFolderPath, "old-name.txt");
        string newFilePath = Path.Combine(watchedFolderPath, "new-name.txt");

        await File.WriteAllTextAsync(newFilePath, "Content.");
        var (service, provider, _) = CreateService(database);

        // We skip calling HandleCreatedOrChangedAsync for oldFilePath to simulate missing link
        await service.HandleRenamedAsync(oldFilePath, newFilePath, watchedFolderPath);

        Domain.Entities.Document document = await database.Context.Documents.SingleAsync();
        Domain.Entities.WatchedFileLink link = await database.Context.WatchedFileLinks.SingleAsync();

        Assert.Equal("new-name.txt", document.Name);
        Assert.Equal(newFilePath, link.FilePath);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (string path in pathsToDelete.OrderByDescending(path => path.Length))
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            else if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        await Task.CompletedTask;
    }

    private (
        WatchedFileIngestionService Service,
        MutableDateTimeProvider DateTimeProvider,
        FakeIngestionJobSignal Signal) CreateService(TestDatabase database)
    {
        var provider = new MutableDateTimeProvider();
        FakeIngestionJobSignal signal = new();
        var service = new WatchedFileIngestionService(
            database.Context,
            provider,
            new FakeSettingsService(),
            new FakeWatchedFileFilterService(),
            new FakeFileStorageService(),
            signal);

        return (service, provider, signal);
    }

    private string CreateTempDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "localmind-watched-file-ingestion-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        pathsToDelete.Add(directory);

        return directory;
    }

    private static string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? fullPath.ToUpperInvariant()
            : fullPath;
    }

    private sealed class MutableDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AppSettingsDto(default!, default!, default!, default!, default!, new WatchedFoldersSettingsDto(true, 1000, "MarkDeleted", [])));
        }

        public Task<KnowledgeApp.Application.Common.Results.Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KnowledgeApp.Application.Common.Results.Result.Success());
        }
    }

    private sealed class FakeWatchedFileFilterService : IWatchedFileFilterService
    {
        public WatchedFileFilterContext CreateContext(WatchedFoldersSettingsDto settings)
        {
            return new WatchedFileFilterContext(settings);
        }

        public WatchedFileFilterResult Evaluate(string filePath, WatchedFileFilterContext context)
        {
            if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.UnsupportedExtension);
            }
            return WatchedFileFilterResult.Allowed();
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
            SqliteConnection connection = new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            AppDbContext context = new AppDbContext(options);

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
