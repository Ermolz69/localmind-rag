using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionQueue(
    IIngestionJobRepository ingestionJobs,
    IDateTimeProvider dateTimeProvider) : IIngestionQueue
{
    public async Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await ingestionJobs.CreatePendingAsync(documentId, dateTimeProvider.UtcNow, cancellationToken);
    }
}
