namespace LocalMind.Sync.Application.Conflicts;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Common;
using LocalMind.Sync.Application.Mappers;
using LocalMind.Sync.Contracts.Conflicts;
using LocalMind.Sync.Domain.Conflicts;

public sealed class ConflictService
{
    private readonly IClock clock;
    private readonly IConflictRepository conflicts;
    private readonly ISyncQueuePublisher queuePublisher;

    public ConflictService(IConflictRepository conflicts, ISyncQueuePublisher queuePublisher, IClock clock)
    {
        this.conflicts = conflicts;
        this.queuePublisher = queuePublisher;
        this.clock = clock;
    }

    public async Task<Result<IReadOnlyList<ConflictResponse>>> ListOpenAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SyncConflict> openConflicts = await conflicts.ListOpenAsync(cancellationToken);
        return Result<IReadOnlyList<ConflictResponse>>.Success(openConflicts.Select(ContractMappers.ToResponse).ToArray());
    }

    public async Task<Result<ConflictResponse>> ResolveAsync(Guid conflictId, ResolveConflictRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Strategy, ignoreCase: true, out ConflictResolutionStrategy _))
        {
            return Result<ConflictResponse>.Failure(ApplicationError.Validation("Conflict resolution strategy is invalid", new Dictionary<string, string> { ["strategy"] = "Unsupported strategy" }));
        }

        SyncConflict? conflict = await conflicts.FindByIdAsync(conflictId, cancellationToken);
        if (conflict is null)
        {
            return Result<ConflictResponse>.Failure(ApplicationError.NotFound("SYNC_CONFLICT_NOT_FOUND", "Sync conflict was not found"));
        }

        SyncConflict resolved = await conflicts.SaveAsync(conflict.Resolve(clock.UtcNow), cancellationToken);
        await queuePublisher.PublishConflictDetectedAsync(conflictId, request.Strategy, cancellationToken);
        return Result<ConflictResponse>.Success(ContractMappers.ToResponse(resolved));
    }
}
