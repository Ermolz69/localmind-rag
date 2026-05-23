using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class QueuedIngestionJobDispatcher(
    AppDbContext dbContext,
    IIngestionJobProcessor processor,
    IOptions<IngestionWorkerOptions> options,
    ILogger<QueuedIngestionJobDispatcher> logger)
{
    private readonly IngestionWorkerOptions options = options.Value;

    public async Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int batchSize = Math.Max(1, options.BatchSize);
        IngestionJob[] queuedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.Status == IngestionJobStatus.Queued)
            .ToArrayAsync(cancellationToken);
        Guid[] jobIds = queuedJobs
            .OrderBy(job => job.CreatedAt)
            .ThenBy(job => job.Id)
            .Take(batchSize)
            .Select(job => job.Id)
            .ToArray();

        int processedCount = 0;
        foreach (Guid jobId in jobIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Processing queued ingestion job {JobId}.", jobId);
                await processor.ProcessAsync(jobId, cancellationToken);
                processedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Queued ingestion job {JobId} failed before processor could store failure state.", jobId);
            }
        }

        return processedCount;
    }
}
