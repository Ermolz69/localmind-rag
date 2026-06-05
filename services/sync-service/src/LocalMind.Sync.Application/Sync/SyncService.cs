namespace LocalMind.Sync.Application.Sync;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Common;
using LocalMind.Sync.Application.Mappers;
using LocalMind.Sync.Contracts.Sync;
using LocalMind.Sync.Domain.Changes;
using LocalMind.Sync.Domain.Manifests;

public sealed class SyncService
{
    private const int DefaultPullLimit = 100;
    private const int MaxPullLimit = 500;
    private static readonly TimeSpan DeviceLockTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    private readonly IChangeRepository changes;
    private readonly IClock clock;
    private readonly IDeviceRepository devices;
    private readonly IDistributedLockService locks;
    private readonly IIdempotencyStore idempotency;
    private readonly IManifestRepository manifests;
    private readonly ManifestDiffCalculator manifestDiffCalculator;
    private readonly ISyncQueuePublisher queuePublisher;

    public SyncService(
        IDeviceRepository devices,
        IManifestRepository manifests,
        IChangeRepository changes,
        IDistributedLockService locks,
        IIdempotencyStore idempotency,
        ISyncQueuePublisher queuePublisher,
        ManifestDiffCalculator manifestDiffCalculator,
        IClock clock)
    {
        this.devices = devices;
        this.manifests = manifests;
        this.changes = changes;
        this.locks = locks;
        this.idempotency = idempotency;
        this.queuePublisher = queuePublisher;
        this.manifestDiffCalculator = manifestDiffCalculator;
        this.clock = clock;
    }

    public async Task<Result<ManifestDiffResponse>> SubmitManifestAsync(SubmitManifestRequest request, CancellationToken cancellationToken)
    {
        if (await devices.FindByIdAsync(request.DeviceId, cancellationToken) is null)
        {
            return Result<ManifestDiffResponse>.Failure(ApplicationError.NotFound("DEVICE_NOT_FOUND", "Device was not found"));
        }

        SyncManifest manifest = new(request.DeviceId, request.Items.Select(ContractMappers.ToDomain).ToArray(), clock.UtcNow);
        SyncManifest? remoteManifest = await manifests.FindLatestByDeviceIdAsync(request.DeviceId, cancellationToken);
        ManifestDiff diff = manifestDiffCalculator.Calculate(manifest, remoteManifest);
        await manifests.SaveAsync(manifest, cancellationToken);
        await queuePublisher.PublishDiffRequestedAsync(request.DeviceId, cancellationToken);

        ManifestDiffResponse response = new(
            request.DeviceId,
            diff.MissingRemote.Select(ContractMappers.ToDto).ToArray(),
            diff.MissingLocal.Select(ContractMappers.ToDto).ToArray(),
            diff.Diverged.Select(ContractMappers.ToDto).ToArray());

        return Result<ManifestDiffResponse>.Success(response);
    }

    public async Task<Result<PushResponse>> PushAsync(PushRequest request, CancellationToken cancellationToken)
    {
        if (await devices.FindByIdAsync(request.DeviceId, cancellationToken) is null)
        {
            return Result<PushResponse>.Failure(ApplicationError.NotFound("DEVICE_NOT_FOUND", "Device was not found"));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Result<PushResponse>.Failure(ApplicationError.Validation("Idempotency key is required", new Dictionary<string, string> { ["idempotencyKey"] = "Required" }));
        }

        if (!await idempotency.TryBeginAsync($"push:{request.DeviceId:N}:{request.IdempotencyKey}", IdempotencyTtl, cancellationToken))
        {
            return Result<PushResponse>.Failure(ApplicationError.Conflict("IDEMPOTENCY_REPLAY", "Push request was already accepted"));
        }

        await using IAsyncDisposable? deviceLock = await locks.TryAcquireAsync($"sync:device:{request.DeviceId:N}", DeviceLockTtl, cancellationToken);
        if (deviceLock is null)
        {
            return Result<PushResponse>.Failure(ApplicationError.Conflict("DEVICE_SYNC_LOCKED", "Device already has an active sync operation"));
        }

        IReadOnlyList<SyncChange> domainChanges = request.Changes.Select(change => ContractMappers.ToDomain(change, request.DeviceId)).ToArray();
        await changes.SaveManyAsync(domainChanges, cancellationToken);
        Guid messageId = await queuePublisher.PublishPushRequestedAsync(request.DeviceId, domainChanges.Count, cancellationToken);
        return Result<PushResponse>.Success(new PushResponse(request.DeviceId, domainChanges.Count, messageId.ToString("N")));
    }

    public async Task<Result<PullResponse>> PullAsync(PullRequest request, CancellationToken cancellationToken)
    {
        if (await devices.FindByIdAsync(request.DeviceId, cancellationToken) is null)
        {
            return Result<PullResponse>.Failure(ApplicationError.NotFound("DEVICE_NOT_FOUND", "Device was not found"));
        }

        int limit = request.Limit <= 0 ? DefaultPullLimit : Math.Min(request.Limit, MaxPullLimit);
        IReadOnlyList<SyncChange> pulled = await changes.PullAsync(request.DeviceId, request.Cursor, limit, cancellationToken);
        await queuePublisher.PublishPullRequestedAsync(request.DeviceId, limit, cancellationToken);

        string nextCursor = pulled.Count == 0 ? request.Cursor : pulled[^1].CreatedAt.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Result<PullResponse>.Success(new PullResponse(request.DeviceId, nextCursor, pulled.Select(ContractMappers.ToDto).ToArray()));
    }
}
