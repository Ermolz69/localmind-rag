using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Observability;

/// <summary>Deletes Serilog log files from the configured logs folder.</summary>
public sealed class LogMaintenanceService(
    IOptions<ObservabilityOptions> options,
    ILogger<LogMaintenanceService> logger) : ILogMaintenanceService
{
    private static readonly string[] LogExtensions = [".log", ".ndjson"];

    public Task<LogCleanupResultDto> CleanupAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteFiles(EnumerateLogFiles(), cancellationToken));
    }

    public Task<LogCleanupResultDto> RemoveOlderThanAsync(
        int retainedDays,
        CancellationToken cancellationToken = default)
    {
        if (retainedDays <= 0)
        {
            return Task.FromResult(new LogCleanupResultDto(0, 0, 0));
        }

        DateTime threshold = DateTime.UtcNow.AddDays(-retainedDays);
        IEnumerable<FileInfo> stale = EnumerateLogFiles()
            .Where(file => file.LastWriteTimeUtc < threshold);

        return Task.FromResult(DeleteFiles(stale, cancellationToken));
    }

    private IEnumerable<FileInfo> EnumerateLogFiles()
    {
        string logsPath = options.Value.LogsPath;

        if (string.IsNullOrWhiteSpace(logsPath) || !Directory.Exists(logsPath))
        {
            return [];
        }

        return new DirectoryInfo(logsPath)
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Where(file => LogExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase));
    }

    private LogCleanupResultDto DeleteFiles(IEnumerable<FileInfo> files, CancellationToken cancellationToken)
    {
        int deleted = 0;
        int skipped = 0;
        long freed = 0;

        foreach (FileInfo file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                long size = file.Length;
                file.Delete();
                deleted++;
                freed += size;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The active log file is held open by the logger; skip files we cannot remove.
                skipped++;
            }
        }

        if (deleted > 0 || skipped > 0)
        {
            logger.LogInformation(
                "Log cleanup removed {DeletedFiles} files ({FreedBytes} bytes); skipped {SkippedFiles}.",
                deleted,
                freed,
                skipped);
        }

        return new LogCleanupResultDto(deleted, freed, skipped);
    }
}
