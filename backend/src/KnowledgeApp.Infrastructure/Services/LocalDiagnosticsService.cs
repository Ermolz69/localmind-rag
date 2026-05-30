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
        DiagnosticsDatabaseDto database = await GetDatabaseAsync(cancellationToken);
        DiagnosticsStorageDto storage = await GetStorageAsync(cancellationToken);
        DiagnosticsVectorIndexDto vectorIndex = await GetVectorIndexAsync(cancellationToken);
        DiagnosticsRuntimeDto runtime = await GetRuntimeAsync(cancellationToken);
        IReadOnlyList<DiagnosticsIngestionErrorDto> latestErrors = await GetLatestErrorsAsync(cancellationToken);

        return new DiagnosticsDto(
            Database: database,
            Storage: storage,
            VectorIndex: vectorIndex,
            Runtime: runtime,
            LatestErrors: latestErrors);
    }

    public async Task<DiagnosticsDatabaseDto> GetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        DiagnosticsHealthStatus status = DiagnosticsHealthStatus.Healthy;
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                status = DiagnosticsHealthStatus.Unhealthy;
            }
        }
        catch
        {
            status = DiagnosticsHealthStatus.Unhealthy;
        }

        if (status == DiagnosticsHealthStatus.Unhealthy)
        {
            return new DiagnosticsDatabaseDto(
                Status: status,
                BucketsCount: 0,
                DocumentsCount: 0,
                DocumentFilesCount: 0,
                NotesCount: 0,
                ConversationsCount: 0,
                PendingIngestionJobsCount: 0,
                RunningIngestionJobsCount: 0,
                FailedIngestionJobsCount: 0,
                CancelledIngestionJobsCount: 0,
                LastProcessedIngestionJobId: null);
        }

        int pendingJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Pending,
            cancellationToken);

        int runningJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Processing
                || job.Status == IngestionJobStatus.Chunking
                || job.Status == IngestionJobStatus.Embedding,
            cancellationToken);

        int failedJobsCount = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Failed,
            cancellationToken);

        int cancelledJobs = await dbContext.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Cancelled,
            cancellationToken);

        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.
        // We fetch a limited subset and sort in memory.
        Domain.Entities.IngestionJob[] processedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.ProcessedAt != null)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        Guid? lastProcessedJobId = processedJobs
            .OrderByDescending(job => job.ProcessedAt)
            .Select(job => (Guid?)job.Id)
            .FirstOrDefault();

        return new DiagnosticsDatabaseDto(
            Status: status,
            BucketsCount: await dbContext.Buckets.CountAsync(cancellationToken),
            DocumentsCount: await dbContext.Documents.CountAsync(cancellationToken),
            DocumentFilesCount: await dbContext.DocumentFiles.CountAsync(cancellationToken),
            NotesCount: await dbContext.Notes.CountAsync(cancellationToken),
            ConversationsCount: await dbContext.Conversations.CountAsync(cancellationToken),
            PendingIngestionJobsCount: pendingJobs,
            RunningIngestionJobsCount: runningJobs,
            FailedIngestionJobsCount: failedJobsCount,
            CancelledIngestionJobsCount: cancelledJobs,
            LastProcessedIngestionJobId: lastProcessedJobId);
    }

    public async Task<DiagnosticsStorageDto> GetStorageAsync(CancellationToken cancellationToken = default)
    {
        bool dbExists = File.Exists(paths.DatabasePath);
        bool filesExist = Directory.Exists(paths.FilesDirectory);
        bool indexExist = Directory.Exists(paths.IndexDirectory);

        DiagnosticsHealthStatus status = DiagnosticsHealthStatus.Healthy;
        if (!dbExists || !filesExist || !indexExist)
        {
            status = DiagnosticsHealthStatus.Degraded;
        }

        return new DiagnosticsStorageDto(
            Status: status,
            DatabaseSizeBytes: SafeFileSize(paths.DatabasePath),
            FilesSizeBytes: SafeDirectorySize(paths.FilesDirectory),
            IndexSizeBytes: SafeDirectorySize(paths.IndexDirectory),
            LogsSizeBytes: SafeDirectorySize(paths.LogsDirectory));
    }

    public async Task<DiagnosticsVectorIndexDto> GetVectorIndexAsync(CancellationToken cancellationToken = default)
    {
        // Vector index status is currently tied to DB connectivity and existence of index directory
        bool indexExist = Directory.Exists(paths.IndexDirectory);
        DiagnosticsHealthStatus status = indexExist ? DiagnosticsHealthStatus.Healthy : DiagnosticsHealthStatus.Degraded;

        try
        {
            return new DiagnosticsVectorIndexDto(
                Status: status,
                DocumentChunksCount: await dbContext.DocumentChunks.CountAsync(cancellationToken),
                DocumentEmbeddingsCount: await dbContext.DocumentEmbeddings.CountAsync(cancellationToken));
        }
        catch
        {
            return new DiagnosticsVectorIndexDto(
                Status: DiagnosticsHealthStatus.Unhealthy,
                DocumentChunksCount: 0,
                DocumentEmbeddingsCount: 0);
        }
    }

    public async Task<DiagnosticsRuntimeDto> GetRuntimeAsync(CancellationToken cancellationToken = default)
    {
        RuntimeStatusDto aiRuntimeStatus = await GetAiRuntimeStatusAsync(cancellationToken);

        DiagnosticsHealthStatus status = DiagnosticsHealthStatus.Healthy;
        if (aiRuntimeStatus.AiRuntimeStatus == "Missing" || !aiRuntimeStatus.ModelsAvailable)
        {
            status = DiagnosticsHealthStatus.Degraded;
        }

        return new DiagnosticsRuntimeDto(
            Status: status,
            RuntimeMode: runtimeModeOptions.Value.Portable ? "portable" : "dev",
            LocalApiVersion: GetLocalApiVersion(),
            AiRuntimeStatus: aiRuntimeStatus);
    }

    public async Task<DiagnosticsHealthStatus> GetGeneralHealthAsync(CancellationToken cancellationToken = default)
    {
        DiagnosticsDto diagnostics = await GetAsync(cancellationToken);
        return diagnostics.Status;
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

    private async Task<IReadOnlyList<DiagnosticsIngestionErrorDto>> GetLatestErrorsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Pull a small amount of failed jobs and sort them in memory to avoid SQLite DateTimeOffset sorting issues.
            Domain.Entities.IngestionJob[] failedJobs = await dbContext.IngestionJobs
                .AsNoTracking()
                .Where(job => job.Status == IngestionJobStatus.Failed && job.ErrorMessage != null)
                .Take(100)
                .ToArrayAsync(cancellationToken);

            failedJobs = failedJobs
                .OrderByDescending(job => job.ProcessedAt ?? job.UpdatedAt ?? job.CreatedAt)
                .Take(5)
                .ToArray();

            Guid[] documentIds = failedJobs
                .Select(job => job.DocumentId)
                .Distinct()
                .ToArray();

            Dictionary<Guid, string> documentNames = await dbContext.Documents
                .AsNoTracking()
                .Where(document => documentIds.Contains(document.Id))
                .ToDictionaryAsync(
                    document => document.Id,
                    document => document.Name,
                    cancellationToken);

            return failedJobs
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
        catch
        {
            return Array.Empty<DiagnosticsIngestionErrorDto>();
        }
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
