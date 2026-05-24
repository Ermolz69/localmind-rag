using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class CancelIngestionJobHandler(
    IAppDbContext dbContext,
    IIngestionJobRepository ingestionJobs,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result<IngestionJobActionResponse>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await ingestionJobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Result<IngestionJobActionResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound));
        }

        if (!IngestionJobMapper.CanCancel(job.Status))
        {
            return Result<IngestionJobActionResponse>.Failure(
                ApplicationErrors.Conflict(ErrorCodes.Ingestion.JobNotCancellable, ErrorMessages.Ingestion.JobNotCancellable));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Guid operationId = Guid.NewGuid();
        await ingestionJobs.MarkCancelledAsync(job.Id, operationId, now, cancellationToken);

        Domain.Entities.Document? document = await dbContext.Documents
            .FirstOrDefaultAsync(item => item.Id == job.DocumentId && item.DeletedAt == null, cancellationToken);
        if (document is not null)
        {
            document.Status = DocumentStatus.Queued;
            document.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestionJobActionResponse>.Success(
            new IngestionJobActionResponse(job.Id, IngestionJobStatus.Cancelled.ToString(), ErrorMessages.Ingestion.Cancelled));
    }
}
