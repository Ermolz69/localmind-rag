using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFolderStatusStoreTests : IDisposable
{
    private readonly string rootPath;
    private readonly string watchedFolderPath;

    public WatchedFolderStatusStoreTests()
    {
        rootPath = Path.Combine(
            Path.GetTempPath(),
            "localmind-watched-folder-status-tests",
            Guid.NewGuid().ToString("N"));

        watchedFolderPath = Path.Combine(rootPath, "watch");

        Directory.CreateDirectory(watchedFolderPath);
    }

    [Fact]
    public void GetStatus_Should_ReturnConfiguredFolders()
    {
        WatchedFolderStatusStore store = new WatchedFolderStatusStore();

        WatchedFoldersSettingsDto settings = CreateSettings();

        Contracts.WatchedFolders.WatchedFolderStatusResponse status = store.GetStatus(settings);

        Assert.True(status.Enabled);
        Assert.Equal(1000, status.DebounceMilliseconds);
        Assert.Equal("MarkDeleted", status.DeletePolicy);
        Assert.Single(status.Folders);

        Contracts.WatchedFolders.WatchedFolderStatusDto folderStatus = status.Folders[0];

        Assert.Equal(watchedFolderPath, folderStatus.Path);
        Assert.True(folderStatus.Enabled);
        Assert.False(folderStatus.IncludeSubdirectories);
        Assert.True(folderStatus.Exists);
        Assert.False(folderStatus.IsWatching);
        Assert.Equal(0, folderStatus.PendingEvents);
        Assert.Null(folderStatus.LastError);
    }

    [Fact]
    public void GetStatus_Should_ReturnFolderRuntimeState()
    {
        WatchedFolderStatusStore store = new WatchedFolderStatusStore();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        store.SetFolderWatching(watchedFolderPath, isWatching: true);
        store.SetFolderPendingEvents(watchedFolderPath, pendingEvents: 3);
        store.RecordFolderEvent(watchedFolderPath, now);

        Contracts.WatchedFolders.WatchedFolderStatusResponse status = store.GetStatus(CreateSettings());

        Contracts.WatchedFolders.WatchedFolderStatusDto folderStatus = status.Folders[0];

        Assert.True(folderStatus.IsWatching);
        Assert.Equal(3, folderStatus.PendingEvents);
        Assert.Equal(now, folderStatus.LastEventAt);
    }

    [Fact]
    public void GetStatus_Should_ReturnSanitizedFolderErrors()
    {
        WatchedFolderStatusStore store = new WatchedFolderStatusStore();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        string longError = new string('x', 300);

        store.RecordFolderError(watchedFolderPath, longError, now);

        Contracts.WatchedFolders.WatchedFolderStatusResponse status = store.GetStatus(CreateSettings());

        Contracts.WatchedFolders.WatchedFolderStatusDto folderStatus = status.Folders[0];

        Assert.NotNull(folderStatus.LastError);
        Assert.True(folderStatus.LastError.Length <= 200);
        Assert.Equal(now, folderStatus.LastErrorAt);
    }

    [Fact]
    public void GetStatus_Should_ReturnGlobalPendingEventsAndErrors()
    {
        WatchedFolderStatusStore store = new WatchedFolderStatusStore();
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        store.SetGlobalPendingEvents(5);
        store.RecordGlobalError("Unable to watch folder.", now);

        Contracts.WatchedFolders.WatchedFolderStatusResponse status = store.GetStatus(CreateSettings());

        Assert.Equal(5, status.PendingEvents);
        Assert.Equal("Unable to watch folder.", status.LastError);
        Assert.Equal(now, status.LastErrorAt);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            try
            {
                Directory.Delete(rootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private WatchedFoldersSettingsDto CreateSettings()
    {
        return new WatchedFoldersSettingsDto(
            Enabled: true,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders:
            [
                new WatchedFolderDto(
                    Path: watchedFolderPath,
                    Enabled: true,
                    IncludeSubdirectories: false)
            ]);
    }
}
