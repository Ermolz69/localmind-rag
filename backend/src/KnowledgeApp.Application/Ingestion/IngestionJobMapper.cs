using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Ingestion;

internal static class IngestionJobMapper
{
    public static IngestionJobDto ToDto(IngestionJob job)
    {
        return new IngestionJobDto(
            job.Id,
            job.DocumentId,
            job.Status.ToString(),
            job.CreatedAt,
            job.UpdatedAt,
            job.ProcessedAt,
            job.LastError,
            job.AttemptCount,
            CanRetry(job.Status),
            CanCancel(job.Status),
            job.LastOperationId);
    }

    public static bool CanRetry(IngestionJobStatus status)
    {
        return status is IngestionJobStatus.Failed or IngestionJobStatus.Cancelled;
    }

    public static bool CanCancel(IngestionJobStatus status)
    {
        return status is IngestionJobStatus.Queued or IngestionJobStatus.Running;
    }
}
