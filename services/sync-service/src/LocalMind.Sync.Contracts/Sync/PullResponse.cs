namespace LocalMind.Sync.Contracts.Sync;

public sealed record PullResponse(Guid DeviceId, string Cursor, IReadOnlyList<SyncChangeDto> Changes);
