using System.Reflection;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class LocalDiagnosticsService(
    IAppPathProvider paths,
    IOptions<RuntimeModeOptions> runtimeModeOptions,
    IAiRuntimeProviderRegistry aiRuntimeProviders,
    AppDbContext dbContext) : ILocalDiagnosticsService
{
    public async Task<DiagnosticsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        RuntimeStatusDto? aiRuntimeStatus = await GetAiRuntimeStatusAsync(cancellationToken);
        DiagnosticsCountsDto? counts = await GetCountsAsync(cancellationToken);
        IReadOnlyList<DiagnosticsIngestionErrorDto>? latestErrors =
            await GetLatestErrorsAsync(cancellationToken);

        return new DiagnosticsDto(
            Paths: new DiagnosticsPathsDto(
                DatabasePath: paths.DatabasePath,
                FilesPath: paths.FilesDirectory,
                IndexPath: paths.IndexDirectory,
                LogsPath: paths.LogsDirectory),
            Storage: new DiagnosticsStorageDto(
                DatabaseSizeBytes: SafeFileSize(paths.DatabasePath),
                FilesSizeBytes: SafeDirectorySize(paths.FilesDirectory),
                IndexSizeBytes: SafeDirectorySize(paths.IndexDirectory),
                LogsSizeBytes: SafeDirectorySize(paths.LogsDirectory)),
            Counts: counts,
            LatestErrors: latestErrors,
            Runtime: new DiagnosticsRuntimeDto(
                RuntimeMode: runtimeModeOptions.Value.Portable ? "portable" : "dev",
                LocalApiVersion: GetLocalApiVersion(),
                AiRuntimeStatus: aiRuntimeStatus));
    }

    private async Task<RuntimeStatusDto> GetAiRuntimeStatusAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await aiRuntimeProviders
                .GetSelectedProvider()
                .GetStatusAsync(cancellationToken);
        }
        catch
        {
            return new RuntimeStatusDto(
                LocalApiReady: true,
                AiRuntimeStatus: "Missing",
                ModelsAvailable: false,
                OfflineMode: true);
        }
    }

    private async Task<DiagnosticsCountsDto> GetCountsAsync(
        CancellationToken cancellationToken)
    {
        int pendingJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Pending,
            cancellationToken);

        int runningJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Processing
                || job.Status == IngestionJobStatus.Chunking
                || job.Status == IngestionJobStatus.Embedding,
            cancellationToken);

        int failedJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Failed,
            cancellationToken);

        int cancelledJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Cancelled,
            cancellationToken);

        Domain.Entities.IngestionJob[] processedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.ProcessedAt != null)
            .ToArrayAsync(cancellationToken);

        Guid? lastProcessedJobId = processedJobs
            .OrderByDescending(job => job.ProcessedAt)
            .Select(job => (Guid?)job.Id)
            .FirstOrDefault();

        return new DiagnosticsCountsDto(
            BucketsCount: await dbContext.Buckets.CountAsync(cancellationToken),
            DocumentsCount: await dbContext.Documents.CountAsync(cancellationToken),
            DocumentFilesCount: await dbContext.DocumentFiles.CountAsync(cancellationToken),
            DocumentChunksCount: await dbContext.DocumentChunks.CountAsync(cancellationToken),
            DocumentEmbeddingsCount: await dbContext.DocumentEmbeddings.CountAsync(cancellationToken),
            NotesCount: await dbContext.Notes.CountAsync(cancellationToken),
            ConversationsCount: await dbContext.Conversations.CountAsync(cancellationToken),
            PendingIngestionJobsCount: pendingJobs,
            FailedIngestionJobsCount: failedJobs,
            RunningIngestionJobsCount: runningJobs,
            CancelledIngestionJobsCount: cancelledJobs,
            LastProcessedIngestionJobId: lastProcessedJobId);
    }

    private async Task<IReadOnlyList<DiagnosticsIngestionErrorDto>> GetLatestErrorsAsync(
        CancellationToken cancellationToken)
    {
        Domain.Entities.IngestionJob[]? failedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.Status == IngestionJobStatus.Failed && job.ErrorMessage != null)
            .ToArrayAsync(cancellationToken);

        Guid[]? documentIds = failedJobs
            .Select(job => job.DocumentId)
            .Distinct()
            .ToArray();

        Dictionary<Guid, string>? documentNames = await dbContext.Documents
            .AsNoTracking()
            .Where(document => documentIds.Contains(document.Id))
            .ToDictionaryAsync(
                document => document.Id,
                document => document.Name,
                cancellationToken);

        return failedJobs
            .OrderByDescending(job => job.ProcessedAt ?? job.UpdatedAt ?? job.CreatedAt)
            .Take(5)
            .Select(job => new DiagnosticsIngestionErrorDto(
                JobId: job.Id,
                DocumentId: job.DocumentId,
                DocumentName: documentNames.GetValueOrDefault(job.DocumentId, "Unknown document"),
                ErrorCode: job.ErrorCode ?? "INGESTION_JOB_FAILED",
                ErrorMessage: job.ErrorMessage ?? string.Empty,
                ProcessedAt: job.ProcessedAt,
                RetryCount: job.RetryCount,
                LastOperationId: job.LastOperationId))
            .ToArray();
    }

    private static string GetLocalApiVersion()
    {
        Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == "KnowledgeApp.LocalApi");

        return assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly?.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static long SafeFileSize(string filePath)
    {
        try
        {
            return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static long SafeDirectorySize(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return 0;
            }

            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Sum(SafeFileSize);
        }
        catch
        {
            return 0;
        }
    }
}
