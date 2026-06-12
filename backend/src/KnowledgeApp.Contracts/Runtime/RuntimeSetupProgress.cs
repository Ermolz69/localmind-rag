namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Represents a progress update during AI runtime setup.</summary>
/// <param name="Stage">The current setup stage (e.g., checking, downloading-runtime, extracting-runtime).</param>
/// <param name="Message">A human-readable description of the current progress.</param>
/// <param name="DownloadedBytes">The number of bytes downloaded so far, if applicable.</param>
/// <param name="TotalBytes">The total number of bytes to download, if known.</param>
/// <param name="SpeedBytesPerSecond">The current download speed in bytes per second, if applicable.</param>
/// <param name="IsCompleted">True if the setup has successfully completed.</param>
/// <param name="IsFailed">True if the setup has failed.</param>
public sealed record RuntimeSetupProgress(
    string Stage,
    string Message,
    long? DownloadedBytes = null,
    long? TotalBytes = null,
    double? SpeedBytesPerSecond = null,
    bool IsCompleted = false,
    bool IsFailed = false);
