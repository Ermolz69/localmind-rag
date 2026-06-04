namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Conflicts;
using LocalMind.Sync.Contracts.Conflicts;
using LocalMind.Sync.Domain.Conflicts;
using Xunit;

public sealed class ConflictServiceQueuePublishingTests
{
    [Fact]
    public async Task ResolvePublishesTypedConflictDetectedMessage()
    {
        Guid conflictId = Guid.NewGuid();
        QueuePublisher publisher = new();
        ConflictService service = new(new Conflicts(conflictId), publisher, new Clock());

        var result = await service.ResolveAsync(conflictId, new ResolveConflictRequest("KeepLocal"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(conflictId, publisher.ConflictId);
        Assert.Equal("KeepLocal", publisher.Strategy);
    }

    private sealed class QueuePublisher : ISyncQueuePublisher
    {
        public Guid? ConflictId { get; private set; }

        public string? Strategy { get; private set; }

        public Task<Guid> PublishPushRequestedAsync(Guid deviceId, int changeCount, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> PublishPullRequestedAsync(Guid deviceId, int limit, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> PublishDiffRequestedAsync(Guid deviceId, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid());

        public Task<Guid> PublishConflictDetectedAsync(Guid conflictId, string strategy, CancellationToken cancellationToken)
        {
            ConflictId = conflictId;
            Strategy = strategy;
            return Task.FromResult(Guid.NewGuid());
        }
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-06-05T00:00:00Z");
    }

    private sealed class Conflicts : IConflictRepository
    {
        private readonly Guid conflictId;

        public Conflicts(Guid conflictId)
        {
            this.conflictId = conflictId;
        }

        public Task<SyncConflict?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            SyncConflict? conflict = id == conflictId
                ? SyncConflict.Open(Guid.NewGuid(), "folder", Guid.NewGuid(), 1, 2, DateTimeOffset.Parse("2026-06-05T00:00:00Z"))
                : null;
            return Task.FromResult(conflict);
        }

        public Task<IReadOnlyList<SyncConflict>> ListOpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SyncConflict>>([]);
        }

        public Task<SyncConflict> SaveAsync(SyncConflict conflict, CancellationToken cancellationToken)
        {
            return Task.FromResult(conflict);
        }
    }
}
