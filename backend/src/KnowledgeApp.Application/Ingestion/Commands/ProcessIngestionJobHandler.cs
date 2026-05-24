using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class ProcessIngestionJobHandler(IAppDbContext dbContext, IIngestionJobProcessor processor)
{
    public async Task<Result<ProcessIngestionJobResponse>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await dbContext.IngestionJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null)
        {
            return Result<ProcessIngestionJobResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound));
        }

        if (job.Status == IngestionJobStatus.Running)
        {
            return Result<ProcessIngestionJobResponse>.Failure(
                ApplicationErrors.Conflict(ErrorCodes.Ingestion.JobAlreadyRunning, ErrorMessages.Ingestion.JobAlreadyRunning));
        }

        await processor.ProcessAsync(jobId, cancellationToken);
        job = await dbContext.IngestionJobs
            .AsNoTracking()
            .SingleAsync(item => item.Id == jobId, cancellationToken);
        return Result<ProcessIngestionJobResponse>.Success(new ProcessIngestionJobResponse(jobId, job.Status.ToString()));
    }
}
