using System.Collections.Concurrent;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public sealed class WatchedFolderStatusStore : IWatchedFolderStatusStore
{
    private readonly ConcurrentDictionary<string, FolderState> folderStates = new(PathComparer);

    private int globalPendingEvents;
    private string? lastGlobalError;
    private DateTimeOffset? lastGlobalErrorAt;

    public WatchedFolderStatusResponse GetStatus(WatchedFoldersSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        List<WatchedFolderStatusDto> folders = [];

        foreach (WatchedFolderDto folder in settings.Folders)
        {
            string normalizedPath = NormalizePathOrFallback(folder.Path);

            folderStates.TryGetValue(normalizedPath, out FolderState? state);

            folders.Add(new WatchedFolderStatusDto(
                Path: folder.Path,
                Enabled: folder.Enabled,
                IncludeSubdirectories: folder.IncludeSubdirectories,
                Exists: Directory.Exists(folder.Path),
                IsWatching: state?.IsWatching ?? false,
                PendingEvents: state?.PendingEvents ?? 0,
                LastEventAt: state?.LastEventAt,
                LastError: state?.LastError,
                LastErrorAt: state?.LastErrorAt));
        }

        return new WatchedFolderStatusResponse(
            Enabled: settings.Enabled,
            DebounceMilliseconds: settings.DebounceMilliseconds,
            PendingEvents: globalPendingEvents,
            DeletePolicy: settings.DeletePolicy,
            LastError: lastGlobalError,
            LastErrorAt: lastGlobalErrorAt,
            Folders: folders);
    }

    public void SetFolderWatching(string folderPath, bool isWatching)
    {
        FolderState state = GetOrCreateFolderState(folderPath);

        state.IsWatching = isWatching;
    }

    public void SetFolderPendingEvents(string folderPath, int pendingEvents)
    {
        FolderState state = GetOrCreateFolderState(folderPath);

        state.PendingEvents = Math.Max(0, pendingEvents);
    }

    public void RecordFolderEvent(string folderPath, DateTimeOffset occurredAt)
    {
        FolderState state = GetOrCreateFolderState(folderPath);

        state.LastEventAt = occurredAt;
    }

    public void RecordFolderError(string folderPath, string sanitizedError, DateTimeOffset occurredAt)
    {
        FolderState state = GetOrCreateFolderState(folderPath);

        state.LastError = SanitizeMessage(sanitizedError);
        state.LastErrorAt = occurredAt;
    }

    public void RecordGlobalError(string sanitizedError, DateTimeOffset occurredAt)
    {
        lastGlobalError = SanitizeMessage(sanitizedError);
        lastGlobalErrorAt = occurredAt;
    }

    public void SetGlobalPendingEvents(int pendingEvents)
    {
        globalPendingEvents = Math.Max(0, pendingEvents);
    }

    private FolderState GetOrCreateFolderState(string folderPath)
    {
        string normalizedPath = NormalizePathOrFallback(folderPath);

        return folderStates.GetOrAdd(normalizedPath, _ => new FolderState());
    }

    private static string NormalizePathOrFallback(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            string fullPath = Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return OperatingSystem.IsWindows()
                ? fullPath.ToUpperInvariant()
                : fullPath;
        }
        catch (Exception)
        {
            return path.Trim();
        }
    }

    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Watcher error.";
        }

        string sanitized = message.Trim();

        return sanitized.Length <= 200
            ? sanitized
            : sanitized[..200];
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private sealed class FolderState
    {
        public bool IsWatching { get; set; }

        public int PendingEvents { get; set; }

        public DateTimeOffset? LastEventAt { get; set; }

        public string? LastError { get; set; }

        public DateTimeOffset? LastErrorAt { get; set; }
    }
}
