using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class RetryIngestionJobHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<Result<IngestionJobActionResponse>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await dbContext.IngestionJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null)
        {
            return Result<IngestionJobActionResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound));
        }

        if (!IngestionJobMapper.CanRetry(job.Status))
        {
            return Result<IngestionJobActionResponse>.Failure(
                ApplicationErrors.Conflict(ErrorCodes.Ingestion.JobNotRetryable, ErrorMessages.Ingestion.JobNotRetryable));
        }

        job.Status = IngestionJobStatus.Queued;
        job.LastError = null;
        job.ProcessedAt = null;
        job.UpdatedAt = dateTimeProvider.UtcNow;
        job.LastOperationId = Guid.NewGuid();

        Domain.Entities.Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(item => item.Id == job.DocumentId && item.DeletedAt == null, cancellationToken);
        if (document is not null)
        {
            document.Status = DocumentStatus.Queued;
            document.UpdatedAt = dateTimeProvider.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestionJobActionResponse>.Success(
            new IngestionJobActionResponse(job.Id, job.Status.ToString(), ErrorMessages.Ingestion.RetryQueued));
    }
}
