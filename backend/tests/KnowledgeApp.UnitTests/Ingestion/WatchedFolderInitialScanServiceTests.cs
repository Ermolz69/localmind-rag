using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Services.WatchedFolders;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFolderInitialScanServiceTests : IDisposable
{
    private readonly FakeFileWatcherDebounceBuffer debounceBuffer;
    private readonly FakeDateTimeProvider dateTimeProvider;
    private readonly WatchedFolderInitialScanService service;
    private readonly string tempDirectory;

    public WatchedFolderInitialScanServiceTests()
    {
        debounceBuffer = new FakeFileWatcherDebounceBuffer();
        dateTimeProvider = new FakeDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        service = new WatchedFolderInitialScanService(
            debounceBuffer,
            dateTimeProvider,
            NullLogger<WatchedFolderInitialScanService>.Instance);

        tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task EnqueueInitialFiles_Should_Enqueue_Supported_Files()
    {
        // Arrange
        string supportedFile1 = Path.Combine(tempDirectory, "test.pdf");
        string supportedFile2 = Path.Combine(tempDirectory, "notes.md");
        string unsupportedFile = Path.Combine(tempDirectory, "image.png");

        await File.WriteAllTextAsync(supportedFile1, "pdf content");
        await File.WriteAllTextAsync(supportedFile2, "markdown content");
        await File.WriteAllTextAsync(unsupportedFile, "png content");

        WatchedFolderDto folder = new WatchedFolderDto(
            Path: tempDirectory,
            Enabled: true,
            IncludeSubdirectories: false);

        // Act
        service.EnqueueInitialFiles(folder);

        // Wait a bit since scanning uses Task.Run
        await Task.Delay(200);

        // Assert
        Assert.Contains(debounceBuffer.Changes, c => c.FilePath == supportedFile1 && c.ChangeType == WatchedFileChangeType.CreatedOrChanged);
        Assert.Contains(debounceBuffer.Changes, c => c.FilePath == supportedFile2 && c.ChangeType == WatchedFileChangeType.CreatedOrChanged);
        Assert.DoesNotContain(debounceBuffer.Changes, c => c.FilePath == unsupportedFile);
    }

    [Fact]
    public async Task EnqueueInitialFiles_Should_Respect_IncludeSubdirectories()
    {
        // Arrange
        string subDir = Path.Combine(tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        string rootFile = Path.Combine(tempDirectory, "root.txt");
        string subFile = Path.Combine(subDir, "sub.txt");

        await File.WriteAllTextAsync(rootFile, "root content");
        await File.WriteAllTextAsync(subFile, "sub content");

        WatchedFolderDto folder = new WatchedFolderDto(
            Path: tempDirectory,
            Enabled: true,
            IncludeSubdirectories: true);

        // Act
        service.EnqueueInitialFiles(folder);

        // Wait a bit
        await Task.Delay(200);

        // Assert
        Assert.Contains(debounceBuffer.Changes, c => c.FilePath == rootFile);
        Assert.Contains(debounceBuffer.Changes, c => c.FilePath == subFile);
    }

    [Fact]
    public async Task EnqueueInitialFiles_Should_Ignore_Subdirectories_If_Configured()
    {
        // Arrange
        string subDir = Path.Combine(tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        string rootFile = Path.Combine(tempDirectory, "root.txt");
        string subFile = Path.Combine(subDir, "sub.txt");

        await File.WriteAllTextAsync(rootFile, "root content");
        await File.WriteAllTextAsync(subFile, "sub content");

        WatchedFolderDto folder = new WatchedFolderDto(
            Path: tempDirectory,
            Enabled: true,
            IncludeSubdirectories: false);

        // Act
        service.EnqueueInitialFiles(folder);

        // Wait a bit
        await Task.Delay(200);

        // Assert
        Assert.Contains(debounceBuffer.Changes, c => c.FilePath == rootFile);
        Assert.DoesNotContain(debounceBuffer.Changes, c => c.FilePath == subFile);
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
}
