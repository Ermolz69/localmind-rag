using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class QueuedIngestionJobDispatcher(
    IIngestionJobRepository ingestionJobs,
    IIngestionJobProcessor processor,
    IOptions<IngestionWorkerOptions> options,
    ILogger<QueuedIngestionJobDispatcher> logger,
    IAppDiagnosticLogger? diagnostics = null)
{
    private readonly IngestionWorkerOptions options = options.Value;

    public async Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int batchSize = Math.Max(1, options.BatchSize);
        IReadOnlyList<Guid> jobIds = await ingestionJobs.ListPendingJobIdsAsync(batchSize, cancellationToken);

        int processedCount = 0;
        foreach (Guid jobId in jobIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Processing queued ingestion job {JobId}.", jobId);
                Guid operationId = diagnostics?.BeginOperation(
                    DiagnosticNames.Areas.Ingestion,
                    DiagnosticNames.Operations.IngestionDispatch,
                    new Dictionary<string, object?> { [DiagnosticNames.Properties.JobId] = jobId }) ?? Guid.Empty;
                await processor.ProcessAsync(jobId, cancellationToken);
                diagnostics?.LogStep(operationId, DiagnosticNames.Steps.DispatchCompleted);
                processedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Guid operationId = diagnostics?.BeginOperation(
                    DiagnosticNames.Areas.Ingestion,
                    DiagnosticNames.Operations.IngestionDispatch,
                    new Dictionary<string, object?> { [DiagnosticNames.Properties.JobId] = jobId }) ?? Guid.Empty;
                diagnostics?.LogFailure(operationId, exception);
                logger.LogError(exception, "Queued ingestion job {JobId} failed before processor could store failure state.", jobId);
            }
        }

        return processedCount;
    }
}
