namespace LocalMind.Sync.Contracts.Conflicts;

public sealed record ConflictResponse(
    Guid Id,
    Guid DeviceId,
    string EntityType,
    Guid EntityId,
    long LocalVersion,
    long RemoteVersion,
    string Status,
    DateTimeOffset CreatedAt);
