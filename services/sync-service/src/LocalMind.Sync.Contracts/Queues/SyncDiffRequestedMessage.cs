namespace LocalMind.Sync.Contracts.Queues;

public sealed record SyncDiffRequestedMessage(Guid MessageId, Guid DeviceId, DateTimeOffset RequestedAt);
