namespace LocalMind.Sync.Contracts.Queues;

public sealed record SyncPushRequestedMessage(Guid MessageId, Guid DeviceId, int ChangeCount, DateTimeOffset RequestedAt);
