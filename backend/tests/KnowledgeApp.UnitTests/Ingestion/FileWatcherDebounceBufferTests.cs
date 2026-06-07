using KnowledgeApp.Application.Ingestion.WatchedFolders;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class FileWatcherDebounceBufferTests
{
    [Fact]
    public void DequeueReadyChanges_Should_ReturnEmpty_WhenChangeIsStillInsideDebounceWindow()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: CreatePath("file.txt"),
            WatchedFolderPath: CreatePath("watch"),
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(999),
            TimeSpan.FromMilliseconds(1000));

        Assert.Empty(readyChanges);
        Assert.Equal(1, buffer.PendingCount);
    }

    [Fact]
    public void DequeueReadyChanges_Should_ReturnChange_WhenDebounceWindowPassed()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        string filePath = CreatePath("file.txt");

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: CreatePath("watch"),
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(1000),
            TimeSpan.FromMilliseconds(1000));

        WatchedFileChange readyChange = Assert.Single(readyChanges);

        Assert.Equal(Path.GetFullPath(filePath), readyChange.FilePath);
        Assert.Equal(WatchedFileChangeType.CreatedOrChanged, readyChange.ChangeType);
        Assert.Equal(0, buffer.PendingCount);
    }

    [Fact]
    public void AddOrUpdate_Should_CollapseRapidChanges_ForSameFile()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        string filePath = CreatePath("file.txt");
        string watchedFolderPath = CreatePath("watch");

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now.AddMilliseconds(500)));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(1499),
            TimeSpan.FromMilliseconds(1000));

        Assert.Empty(readyChanges);
        Assert.Equal(1, buffer.PendingCount);

        readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(1500),
            TimeSpan.FromMilliseconds(1000));

        WatchedFileChange readyChange = Assert.Single(readyChanges);

        Assert.Equal(now.AddMilliseconds(500), readyChange.LastEventAt);
        Assert.Equal(WatchedFileChangeType.CreatedOrChanged, readyChange.ChangeType);
        Assert.Equal(0, buffer.PendingCount);
    }

    [Fact]
    public void AddOrUpdate_Should_KeepSeparateEntries_ForDifferentFiles()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: CreatePath("first.txt"),
            WatchedFolderPath: CreatePath("watch"),
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: CreatePath("second.txt"),
            WatchedFolderPath: CreatePath("watch"),
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddSeconds(2),
            TimeSpan.FromSeconds(1));

        Assert.Equal(2, readyChanges.Count);
        Assert.Equal(0, buffer.PendingCount);
    }

    [Fact]
    public void AddOrUpdate_Should_LetDeleteOverridePendingCreatedOrChangedChange()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        string filePath = CreatePath("file.txt");
        string watchedFolderPath = CreatePath("watch");

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.Deleted,
            LastEventAt: now.AddMilliseconds(100)));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(1100),
            TimeSpan.FromMilliseconds(1000));

        WatchedFileChange readyChange = Assert.Single(readyChanges);

        Assert.Equal(WatchedFileChangeType.Deleted, readyChange.ChangeType);
    }

    [Fact]
    public void AddOrUpdate_Should_LetCreatedOrChangedOverridePendingDelete_WhenFileIsRecreated()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        string filePath = CreatePath("file.txt");
        string watchedFolderPath = CreatePath("watch");

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.Deleted,
            LastEventAt: now));

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now.AddMilliseconds(100)));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now.AddMilliseconds(1100),
            TimeSpan.FromMilliseconds(1000));

        WatchedFileChange readyChange = Assert.Single(readyChanges);

        Assert.Equal(WatchedFileChangeType.CreatedOrChanged, readyChange.ChangeType);
    }

    [Fact]
    public void DequeueReadyChanges_Should_TreatNegativeDebounceAsZero()
    {
        FileWatcherDebounceBuffer buffer = new FileWatcherDebounceBuffer();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        buffer.AddOrUpdate(new WatchedFileChange(
            FilePath: CreatePath("file.txt"),
            WatchedFolderPath: CreatePath("watch"),
            ChangeType: WatchedFileChangeType.CreatedOrChanged,
            LastEventAt: now));

        IReadOnlyList<WatchedFileChange> readyChanges = buffer.DequeueReadyChanges(
            now,
            TimeSpan.FromMilliseconds(-1));

        Assert.Single(readyChanges);
        Assert.Equal(0, buffer.PendingCount);
    }

    private static string CreatePath(string fileName)
    {
        return Path.Combine(Path.GetTempPath(), "localmind-debounce-tests", fileName);
    }
}
