namespace LocalMind.Sync.Contracts.Sync;

public sealed record PushRequest(Guid DeviceId, string IdempotencyKey, IReadOnlyList<SyncChangeDto> Changes);
