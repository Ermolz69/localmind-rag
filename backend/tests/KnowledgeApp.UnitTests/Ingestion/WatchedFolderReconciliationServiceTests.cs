using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services.WatchedFolders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFolderReconciliationServiceTests : IAsyncLifetime
{
    private FakeFileWatcherDebounceBuffer debounceBuffer = null!;
    private FakeDateTimeProvider dateTimeProvider = null!;
    private string tempDirectory = null!;
    private TestDatabase database = null!;
    private WatchedFolderReconciliationService service = null!;

    public async Task InitializeAsync()
    {
        debounceBuffer = new FakeFileWatcherDebounceBuffer();
        dateTimeProvider = new FakeDateTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        database = await TestDatabase.CreateAsync();

        ServiceCollection services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => database.Context);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        service = new WatchedFolderReconciliationService(
            scopeFactory,
            debounceBuffer,
            dateTimeProvider,
            NullLogger<WatchedFolderReconciliationService>.Instance);

        tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }

        await database.DisposeAsync();
    }

    [Fact]
    public async Task ReconcileFolderAsync_Should_EnqueueNewFile()
    {
        string filePath = Path.Combine(tempDirectory, "new.txt");
        await File.WriteAllTextAsync(filePath, "new file");

        WatchedFolderDto folder = new WatchedFolderDto(tempDirectory, true, false);

        await service.ReconcileFolderAsync(folder);

        Assert.Contains(debounceBuffer.Changes, c =>
            c.FilePath == filePath &&
            c.ChangeType == WatchedFileChangeType.CreatedOrChanged);
    }

    [Fact]
    public async Task ReconcileFolderAsync_Should_EnqueueChangedFile()
    {
        string filePath = Path.Combine(tempDirectory, "changed.txt");
        await File.WriteAllTextAsync(filePath, "content");

        // Ensure file write time is older than the last seen
        File.SetLastWriteTimeUtc(filePath, new DateTime(2026, 1, 1, 10, 0, 0));

        database.Context.WatchedFileLinks.Add(new WatchedFileLink
        {
            WatchedFolderPath = NormalizePath(tempDirectory),
            FilePath = filePath,
            NormalizedFilePath = NormalizePath(filePath),
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero) // Last seen BEFORE the file write time
        });
        await database.Context.SaveChangesAsync();

        WatchedFolderDto folder = new WatchedFolderDto(tempDirectory, true, false);

        await service.ReconcileFolderAsync(folder);

        Assert.Contains(debounceBuffer.Changes, c =>
            c.FilePath == filePath &&
            c.ChangeType == WatchedFileChangeType.CreatedOrChanged);
    }

    [Fact]
    public async Task ReconcileFolderAsync_Should_NotEnqueueUnchangedFile()
    {
        string filePath = Path.Combine(tempDirectory, "unchanged.txt");
        await File.WriteAllTextAsync(filePath, "content");

        File.SetLastWriteTimeUtc(filePath, new DateTime(2026, 1, 1, 8, 0, 0));

        database.Context.WatchedFileLinks.Add(new WatchedFileLink
        {
            WatchedFolderPath = NormalizePath(tempDirectory),
            FilePath = filePath,
            NormalizedFilePath = NormalizePath(filePath),
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero) // Last seen AFTER the file write time
        });
        await database.Context.SaveChangesAsync();

        WatchedFolderDto folder = new WatchedFolderDto(tempDirectory, true, false);

        await service.ReconcileFolderAsync(folder);

        Assert.DoesNotContain(debounceBuffer.Changes, c => c.FilePath == filePath);
    }

    [Fact]
    public async Task ReconcileFolderAsync_Should_EnqueueDeletedFile()
    {
        string filePath = Path.Combine(tempDirectory, "deleted.txt");
        // We DO NOT create the file on disk

        database.Context.WatchedFileLinks.Add(new WatchedFileLink
        {
            WatchedFolderPath = NormalizePath(tempDirectory),
            FilePath = filePath,
            NormalizedFilePath = NormalizePath(filePath),
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero)
        });
        await database.Context.SaveChangesAsync();

        WatchedFolderDto folder = new WatchedFolderDto(tempDirectory, true, false);

        await service.ReconcileFolderAsync(folder);

        Assert.Contains(debounceBuffer.Changes, c =>
            c.FilePath == filePath &&
            c.ChangeType == WatchedFileChangeType.Deleted);
    }

    [Fact]
    public async Task ReconcileFolderAsync_Should_SkipAlreadyDeletedFile()
    {
        string filePath = Path.Combine(tempDirectory, "deleted.txt");

        database.Context.WatchedFileLinks.Add(new WatchedFileLink
        {
            WatchedFolderPath = NormalizePath(tempDirectory),
            FilePath = filePath,
            NormalizedFilePath = NormalizePath(filePath),
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
            DeletedAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)
        });
        await database.Context.SaveChangesAsync();

        WatchedFolderDto folder = new WatchedFolderDto(tempDirectory, true, false);

        await service.ReconcileFolderAsync(folder);

        Assert.DoesNotContain(debounceBuffer.Changes, c => c.FilePath == filePath);
    }

    private static string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? fullPath.ToUpperInvariant()
            : fullPath;
    }

    private sealed class FakeFileWatcherDebounceBuffer : IFileWatcherDebounceBuffer
    {
        public List<WatchedFileChange> Changes { get; } = new();

        public int PendingCount => Changes.Count;

        public void AddOrUpdate(WatchedFileChange change)
        {
            Changes.Add(change);
        }

        public IReadOnlyList<WatchedFileChange> DequeueReadyChanges(DateTimeOffset now, TimeSpan debounceDelay)
        {
            var ready = Changes.ToList();
            Changes.Clear();
            return ready;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
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
