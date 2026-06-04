namespace LocalMind.Sync.Contracts.Sessions;

public sealed record SyncSessionResponse(
    Guid Id,
    Guid DeviceId,
    string Status,
    string Cursor,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
