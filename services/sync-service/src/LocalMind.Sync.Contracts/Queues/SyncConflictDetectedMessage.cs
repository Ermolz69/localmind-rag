namespace LocalMind.Sync.Contracts.Queues;

public sealed record SyncConflictDetectedMessage(Guid MessageId, Guid ConflictId, string Strategy, DateTimeOffset DetectedAt);
