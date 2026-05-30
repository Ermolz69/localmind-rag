using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class RetryIngestionJobHandler(
    IDocumentRepository documentRepository,
    IIngestionJobRepository ingestionJobs,
    IUnitOfWork unitOfWork,
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

        if (!IngestionJobMapper.CanRetry(job.Status))
        {
            return Result<IngestionJobActionResponse>.Failure(
                ApplicationErrors.Conflict(ErrorCodes.Ingestion.JobNotRetryable, ErrorMessages.Ingestion.JobNotRetryable));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Guid operationId = Guid.NewGuid();
        await ingestionJobs.ResetForRetryAsync(job.Id, operationId, now, cancellationToken);

        Domain.Entities.Document? document = await documentRepository.GetByIdAsync(job.DocumentId, cancellationToken);
        if (document is not null)
        {
            document.Status = DocumentStatus.Queued;
            document.UpdatedAt = now;
            await documentRepository.UpdateAsync(document, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IngestionJobActionResponse>.Success(
            new IngestionJobActionResponse(job.Id, IngestionJobStatus.Pending.ToString(), ErrorMessages.Ingestion.RetryQueued));
    }
}
