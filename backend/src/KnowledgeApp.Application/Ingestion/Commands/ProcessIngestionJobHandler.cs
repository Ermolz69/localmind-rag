using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Ingestion;

public sealed class ProcessIngestionJobHandler(
    IIngestionJobRepository ingestionJobs,
    IIngestionJobProcessor processor)
{
    public async Task<Result<ProcessIngestionJobResponse>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await ingestionJobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Result<ProcessIngestionJobResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound));
        }

        if (job.Status is IngestionJobStatus.Processing or IngestionJobStatus.Chunking or IngestionJobStatus.Embedding)
        {
            return Result<ProcessIngestionJobResponse>.Failure(
                ApplicationErrors.Conflict(ErrorCodes.Ingestion.JobAlreadyRunning, ErrorMessages.Ingestion.JobAlreadyRunning));
        }

        await processor.ProcessAsync(jobId, cancellationToken);
        job = await ingestionJobs.GetAsync(jobId, cancellationToken);
        return Result<ProcessIngestionJobResponse>.Success(new ProcessIngestionJobResponse(
            jobId,
            job?.Status.ToString() ?? IngestionJobStatus.Pending.ToString()));
    }
}
