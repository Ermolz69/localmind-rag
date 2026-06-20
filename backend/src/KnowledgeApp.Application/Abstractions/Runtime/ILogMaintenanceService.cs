using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

/// <summary>Removes log files from the local logs folder.</summary>
public interface ILogMaintenanceService
{
    /// <summary>Removes all log files (skipping any currently held open by the logger).</summary>
    Task<LogCleanupResultDto> CleanupAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes log files last written more than <paramref name="retainedDays"/> days ago.</summary>
    Task<LogCleanupResultDto> RemoveOlderThanAsync(
        int retainedDays,
        CancellationToken cancellationToken = default);
}
