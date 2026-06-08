namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public sealed class FileWatcherDebounceBuffer : IFileWatcherDebounceBuffer
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, WatchedFileChange> pendingChanges = new(PathComparer);

    public int PendingCount
    {
        get
        {
            lock (syncRoot)
            {
                return pendingChanges.Count;
            }
        }
    }

    public void AddOrUpdate(WatchedFileChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (string.IsNullOrWhiteSpace(change.FilePath))
        {
            return;
        }

        string normalizedFilePath;

        try
        {
            normalizedFilePath = NormalizePath(change.FilePath);
        }
        catch (Exception)
        {
            return;
        }

        WatchedFileChange normalizedChange = change with
        {
            FilePath = Path.GetFullPath(change.FilePath),
            WatchedFolderPath = string.IsNullOrWhiteSpace(change.WatchedFolderPath)
                ? string.Empty
                : Path.GetFullPath(change.WatchedFolderPath)
        };

        lock (syncRoot)
        {
            if (!pendingChanges.TryGetValue(normalizedFilePath, out WatchedFileChange? existingChange))
            {
                pendingChanges[normalizedFilePath] = normalizedChange;
                return;
            }

            pendingChanges[normalizedFilePath] = Merge(existingChange, normalizedChange);
        }
    }

    public IReadOnlyList<WatchedFileChange> DequeueReadyChanges(
        DateTimeOffset now,
        TimeSpan debounceDelay)
    {
        if (debounceDelay < TimeSpan.Zero)
        {
            debounceDelay = TimeSpan.Zero;
        }

        lock (syncRoot)
        {
            List<string> readyKeys = pendingChanges
                .Where(item => now - item.Value.LastEventAt >= debounceDelay)
                .Select(item => item.Key)
                .ToList();

            List<WatchedFileChange> readyChanges = readyKeys
                .Select(key => pendingChanges[key])
                .OrderBy(change => change.LastEventAt)
                .ToList();

            foreach (string readyKey in readyKeys)
            {
                pendingChanges.Remove(readyKey);
            }

            return readyChanges;
        }
    }

    private static WatchedFileChange Merge(
        WatchedFileChange existingChange,
        WatchedFileChange incomingChange)
    {
        WatchedFileChangeType changeType = ResolveChangeType(existingChange.ChangeType, incomingChange.ChangeType);

        DateTimeOffset lastEventAt = incomingChange.LastEventAt >= existingChange.LastEventAt
            ? incomingChange.LastEventAt
            : existingChange.LastEventAt;

        string watchedFolderPath = !string.IsNullOrWhiteSpace(incomingChange.WatchedFolderPath)
            ? incomingChange.WatchedFolderPath
            : existingChange.WatchedFolderPath;

        string filePath = !string.IsNullOrWhiteSpace(incomingChange.FilePath)
            ? incomingChange.FilePath
            : existingChange.FilePath;

        return new WatchedFileChange(
            FilePath: filePath,
            WatchedFolderPath: watchedFolderPath,
            ChangeType: changeType,
            LastEventAt: lastEventAt);
    }

    private static WatchedFileChangeType ResolveChangeType(
        WatchedFileChangeType existingChangeType,
        WatchedFileChangeType incomingChangeType)
    {
        if (existingChangeType == WatchedFileChangeType.Deleted &&
            incomingChangeType == WatchedFileChangeType.CreatedOrChanged)
        {
            return WatchedFileChangeType.CreatedOrChanged;
        }

        if (incomingChangeType == WatchedFileChangeType.Deleted)
        {
            return WatchedFileChangeType.Deleted;
        }

        return WatchedFileChangeType.CreatedOrChanged;
    }

    private static string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? fullPath.ToUpperInvariant()
            : fullPath;
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
