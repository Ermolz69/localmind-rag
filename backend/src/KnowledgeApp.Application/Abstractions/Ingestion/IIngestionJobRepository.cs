using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Abstractions;

public interface IIngestionJobRepository
{
    Task<IngestionJob> CreatePendingAsync(Guid documentId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<IngestionJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IngestionJob>> ListAsync(string? status, int limit, int offset, CancellationToken cancellationToken = default);

    Task<int> CountAsync(string? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListPendingJobIdsAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<IngestionJob?> ClaimForProcessingAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task UpdateStepAsync(Guid jobId, IngestionJobStatus status, string currentStep, int progressPercent, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task MarkIndexedAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid jobId, string errorCode, string errorMessage, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task MarkCancelledAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task ResetForRetryAsync(Guid jobId, Guid operationId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
