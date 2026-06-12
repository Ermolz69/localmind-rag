using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionQueue(
    IIngestionJobRepository ingestionJobs,
    IIngestionJobSignal signal,
    IDateTimeProvider dateTimeProvider) : IIngestionQueue
{
    public async Task<Guid> EnqueueAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        IngestionJob job = await ingestionJobs.CreatePendingAsync(
            documentId,
            dateTimeProvider.UtcNow,
            cancellationToken);

        await signal.PublishAsync(job.Id, cancellationToken);
        return job.Id;
    }
}
