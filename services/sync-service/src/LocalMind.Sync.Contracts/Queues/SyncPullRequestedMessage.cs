namespace LocalMind.Sync.Contracts.Queues;

public sealed record SyncPullRequestedMessage(Guid MessageId, Guid DeviceId, int Limit, DateTimeOffset RequestedAt);
