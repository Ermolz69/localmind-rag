using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionJobRepository(AppDbContext dbContext) : IIngestionJobRepository
{
    public async Task<IngestionJob> CreatePendingAsync(Guid documentId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob job = new()
        {
            CreatedAt = now,
            DocumentId = documentId,
            Status = IngestionJobStatus.Pending,
            CurrentStep = "Pending",
            ProgressPercent = 0,
        };

        dbContext.IngestionJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<IngestionJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return dbContext.IngestionJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
    }

    public async Task<IReadOnlyList<IngestionJob>> ListAsync(string? status, int limit, int offset, CancellationToken cancellationToken = default)
    {
        IQueryable<IngestionJob> jobs = ApplyStatus(dbContext.IngestionJobs.AsNoTracking(), status);
        IngestionJob[] rows = await jobs.ToArrayAsync(cancellationToken);
        return rows
            .OrderByDescending(job => job.CreatedAt)
            .ThenBy(job => job.Id)
            .Skip(offset)
            .Take(limit)
            .ToArray();
    }

    public async Task<int> CountAsync(string? status, CancellationToken cancellationToken = default)
    {
        IQueryable<IngestionJob> jobs = ApplyStatus(dbContext.IngestionJobs.AsNoTracking(), status);
        return await jobs.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListPendingJobIdsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.Status == IngestionJobStatus.Pending)
            .OrderBy(job => EF.Property<long>(
                job,
                SearchDateIndexing.CreatedAtUnixTimePropertyName))
            .ThenBy(job => job.Id)
            .Select(job => job.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IngestionJob>> GetFailedJobsForDocumentsAsync(IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        return await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => documentIds.Contains(job.DocumentId) && job.ErrorMessage != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IngestionJob?> ClaimForProcessingAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        int claimed = await dbContext.IngestionJobs
            .Where(job => job.Id == jobId && job.Status == IngestionJobStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, IngestionJobStatus.Processing)
                    .SetProperty(job => job.CurrentStep, "Processing")
                    .SetProperty(job => job.ProgressPercent, 10)
                    .SetProperty(job => job.LastOperationId, operationId)
                    .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

        return claimed == 0
            ? null
            : await dbContext.IngestionJobs
                .AsNoTracking()
                .SingleAsync(job => job.Id == jobId, cancellationToken);
    }

    public async Task UpdateStepAsync(Guid jobId, IngestionJobStatus status, string currentStep, int progressPercent, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob? job = await GetTrackedAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        if (job.Status == IngestionJobStatus.Cancelled)
        {
            return;
        }

        job.Status = status;
        job.CurrentStep = currentStep;
        job.ProgressPercent = ClampProgress(progressPercent);
        job.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkIndexedAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob? job = await GetTrackedAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        if (job.Status == IngestionJobStatus.Cancelled)
        {
            return;
        }

        job.Status = IngestionJobStatus.Indexed;
        job.CurrentStep = "Indexed";
        job.ProgressPercent = 100;
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.ProcessedAt = now;
        job.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid jobId, string errorCode, string errorMessage, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob? job = await GetTrackedAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        if (job.Status == IngestionJobStatus.Cancelled)
        {
            return;
        }

        job.Status = IngestionJobStatus.Failed;
        job.CurrentStep = "Failed";
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorMessage;
        job.ProcessedAt = now;
        job.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCancelledAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob? job = await GetTrackedAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = IngestionJobStatus.Cancelled;
        job.CurrentStep = "Cancelled";
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.ProcessedAt = now;
        job.UpdatedAt = now;
        job.LastOperationId = operationId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetForRetryAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        IngestionJob? job = await GetTrackedAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = IngestionJobStatus.Pending;
        job.CurrentStep = "Pending";
        job.ProgressPercent = 0;
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.ProcessedAt = null;
        job.UpdatedAt = now;
        job.LastOperationId = operationId;
        job.RetryCount++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<IngestionJob> ApplyStatus(IQueryable<IngestionJob> query, string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? query
            : query.Where(job => job.Status == Enum.Parse<IngestionJobStatus>(status, ignoreCase: true));
    }

    private Task<IngestionJob?> GetTrackedAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.IngestionJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
    }

    private static int ClampProgress(int progress)
    {
        return Math.Min(100, Math.Max(0, progress));
    }
}
