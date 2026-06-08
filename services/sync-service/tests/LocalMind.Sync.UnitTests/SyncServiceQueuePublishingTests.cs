namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Sync;
using LocalMind.Sync.Contracts.Sync;
using LocalMind.Sync.Domain.Changes;
using LocalMind.Sync.Domain.Devices;
using LocalMind.Sync.Domain.Manifests;
using Xunit;

public sealed class SyncServiceQueuePublishingTests
{
    [Fact]
    public async Task PushPublishesTypedPushRequestedMessage()
    {
        Guid deviceId = Guid.NewGuid();
        QueuePublisher publisher = new();
        SyncService service = CreateService(deviceId, publisher);
        PushRequest request = new(
            deviceId,
            "idem-key",
            [
                new SyncChangeDto(Guid.NewGuid(), "folder", Guid.NewGuid(), 1, "Created", "{}", DateTimeOffset.Parse("2026-06-05T00:00:00Z")),
            ]);

        var result = await service.PushAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(deviceId, publisher.PushDeviceId);
        Assert.Equal(1, publisher.PushChangeCount);
    }

    [Fact]
    public async Task ManifestPublishesTypedDiffRequestedMessage()
    {
        Guid deviceId = Guid.NewGuid();
        QueuePublisher publisher = new();
        SyncService service = CreateService(deviceId, publisher);

        var result = await service.SubmitManifestAsync(new SubmitManifestRequest(deviceId, []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(deviceId, publisher.DiffDeviceId);
    }

    private static SyncService CreateService(Guid registeredDeviceId, QueuePublisher publisher)
    {
        return new SyncService(
            new Devices(registeredDeviceId),
            new Manifests(),
            new Changes(),
            new Locks(),
            new Idempotency(),
            publisher,
            new ManifestDiffCalculator(),
            new Clock());
    }

    private sealed class QueuePublisher : ISyncQueuePublisher
    {
        public Guid? PushDeviceId { get; private set; }

        public int? PushChangeCount { get; private set; }

        public Guid? DiffDeviceId { get; private set; }

        public Task<Guid> PublishPushRequestedAsync(Guid deviceId, int changeCount, CancellationToken cancellationToken)
        {
            PushDeviceId = deviceId;
            PushChangeCount = changeCount;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<Guid> PublishPullRequestedAsync(Guid deviceId, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<Guid> PublishDiffRequestedAsync(Guid deviceId, CancellationToken cancellationToken)
        {
            DiffDeviceId = deviceId;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<Guid> PublishConflictDetectedAsync(Guid conflictId, string strategy, CancellationToken cancellationToken)
        {
            return Task.FromResult(Guid.NewGuid());
        }
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-06-05T00:00:00Z");
    }

    private sealed class Devices : IDeviceRepository
    {
        private readonly Guid registeredDeviceId;

        public Devices(Guid registeredDeviceId)
        {
            this.registeredDeviceId = registeredDeviceId;
        }

        public Task<Device?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Device? device = id == registeredDeviceId
                ? Device.Register("Desktop", DevicePlatform.Windows, "1.0.0", "public-key", DateTimeOffset.Parse("2026-06-05T00:00:00Z"))
                : null;
            return Task.FromResult(device);
        }

        public Task<Device> SaveAsync(Device device, CancellationToken cancellationToken)
        {
            return Task.FromResult(device);
        }
    }

    private sealed class Manifests : IManifestRepository
    {
        public Task<SyncManifest?> FindLatestByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult<SyncManifest?>(null);
        }

        public Task SaveAsync(SyncManifest manifest, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class Changes : IChangeRepository
    {
        public Task<IReadOnlyList<SyncChange>> PullAsync(Guid deviceId, string cursor, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SyncChange>>([]);
        }

        public Task SaveManyAsync(IReadOnlyList<SyncChange> changes, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class Locks : IDistributedLockService
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult<IAsyncDisposable?>(new Releaser());
        }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class Idempotency : IIdempotencyStore
    {
        public Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
